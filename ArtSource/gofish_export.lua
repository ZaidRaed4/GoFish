-- GoFish export: writes the open sprite to the game's PNG in Assets/Art/Pixel at 100% scale.
--
-- cards.aseprite (frames tagged card_2C ... card_AS): one PNG per tag in Assets/Art/Pixel/Cards.
-- Any other file: frame 1 goes to the PNG with the same name as the file
-- (card_back and card_shadow go to Cards/, everything else to Assets/Art/Pixel/).
-- Hidden layers are left out. Unity reimports the PNGs and keeps their import settings.
--
-- Install: copy this file into Aseprite's scripts folder (File > Scripts > Open Scripts Folder),
-- then File > Scripts > Rescan Scripts Folder. Run it from File > Scripts > gofish_export.
-- Command line: aseprite -b ArtSource/cards.aseprite --script ArtSource/gofish_export.lua

local spr = app.sprite
local ui = app.isUIAvailable

-- Messages are one line or a list of lines
local function say(lines)
  if type(lines) == "string" then lines = { lines } end
  if ui then app.alert{ title = "GoFish export", text = lines } else print(table.concat(lines, "\n")) end
end

local function ask(lines)
  if not ui then
    print(table.concat(lines, "\n"))
    print(app.params.force and "Forced." or "Not exported: add --script-param force=1 to export anyway.")
    return app.params.force ~= nil
  end
  table.insert(lines, "")
  table.insert(lines, "Export anyway?")
  return app.alert{ title = "GoFish export", text = lines, buttons = { "Export", "Cancel" } } == 1
end

if not spr then return say("Open a GoFish .aseprite file first.") end
if spr.filename == "" or not app.fs.isFile(spr.filename) then return say("Save the sprite in ArtSource/ first.") end

-- Assets/Art/Pixel is found by walking up from the sprite's folder (ArtSource/ sits next to Assets/).
-- The "out" script parameter overrides it, for exporting somewhere else.
local function absolute(path)
  if path:match("^%a:[/\\]") or path:match("^[/\\]") then return path end
  return app.fs.normalizePath(app.fs.joinPath(app.fs.currentPath, path))
end

local function findArtDir(dir)
  while dir and dir ~= "" do
    local art = app.fs.joinPath(dir, "Assets", "Art", "Pixel")
    if app.fs.isDirectory(art) then return art end
    local parent = app.fs.filePath(dir)
    if parent == dir then break end
    dir = parent
  end
end

local artDir = app.params.out or findArtDir(app.fs.filePath(absolute(spr.filename)))
if not artDir then return say("Could not find Assets/Art/Pixel above " .. spr.filename) end

local jobs = {}
for _, tag in ipairs(spr.tags) do
  if tag.name:match("^card_") then
    table.insert(jobs, { name = tag.name, frame = tag.fromFrame.frameNumber })
  end
end
if #jobs == 0 then
  table.insert(jobs, { name = app.fs.fileTitle(spr.filename), frame = 1 })
end

-- The game is laid out for the current sizes, and a name it doesn't use would be a new asset
local problems = {}
for _, job in ipairs(jobs) do
  local dir = job.name:match("^card_") and app.fs.joinPath(artDir, "Cards") or artDir
  job.path = app.fs.joinPath(dir, job.name .. ".png")
  if not app.params.out then
    if not app.fs.isFile(job.path) then
      table.insert(problems, job.name .. ".png is not a file the game uses")
    else
      local old = Image{ fromFile = job.path }
      if old.width ~= spr.width or old.height ~= spr.height then
        table.insert(problems, string.format("%s.png is %dx%d, this sprite is %dx%d",
          job.name, old.width, old.height, spr.width, spr.height))
      end
    end
  end
end
if #problems > 0 then
  local lines = {}
  for i = 1, math.min(#problems, 8) do lines[i] = problems[i] end
  if #problems > 8 then table.insert(lines, "... and " .. (#problems - 8) .. " more") end
  if not ask(lines) then return end
end

for _, job in ipairs(jobs) do
  local img = Image(spr.spec)
  img:drawSprite(spr, job.frame)
  img:saveAs(job.path)
end

say(#jobs == 1 and ("Exported " .. jobs[1].path) or ("Exported " .. #jobs .. " PNGs to " .. app.fs.joinPath(artDir, "Cards")))
