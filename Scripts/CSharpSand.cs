using Godot;
using System;

public class CSharpSand : Node2D
{
    private Sandbox sandbox;

    private Sprite sprite = new Sprite();
    private Image image = new Image();

    // These are needed for drawing pixels with the mouse.
    private Vector2 mouseCoords;
    private Vector2 prevMouseCoords;

    // The camera can be set here by the parent node when it's used, so that
    // we can properly calculate the sandbox-local coordinates of the mouse events.
    private Camera2D camera;
    
    public override void _Ready()
    {
        // image.Create(256, 256, false, 0); // 0 is Image.FORMAT_L8
        image.Create(256, 256, false, Image.Format.Rgba8);
        
        ImageTexture imageTexture = new ImageTexture();
        // Second argument indicates flags for this image texture (type uint, or can optionally use Texture.FlagsEnum).
        // Default value enables FLAG_MIPMAPS, FLAG_REPEAT and FLAG_FILTER, but we specifically need to disable them
        // (especially mimmaps, to preserve the pixalated look no matter the scale), so passing 0 here.
        imageTexture.CreateFromImage(image, 0);

        sprite.Texture = imageTexture;
        sprite.Centered = false;
        // Lower ZIndex to display this behind the UI elements
        sprite.ZIndex = -1;
        AddChild(sprite);

        sandbox = new Sandbox(image);
    }

    public override void _Process(float delta)
    {
        ProcessInput();
        sandbox.Process();

        (sprite.Texture as ImageTexture).SetData(image);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            prevMouseCoords = mouseCoords;

            mouseCoords = (@event as InputEventMouseMotion).Position;
            mouseCoords = sprite.ToLocal(mouseCoords);
            if (camera != null)
            {
                mouseCoords *= camera.Zoom;
            }
        }
    }

    public void set_camera(Camera2D camera)
    {
        this.camera = camera;
    }

    private void ProcessInput()
    {
        CellType cellType = CellType.EMPTY;
        if (Input.IsKeyPressed(Convert.ToInt32(KeyList.S)))
        {
            cellType = CellType.SAND;
        }
        else if (Input.IsKeyPressed(Convert.ToInt32(KeyList.W)))
        {
            cellType = CellType.WATER;
        }
        else  if (Input.IsKeyPressed(Convert.ToInt32(KeyList.Q)))
        {
            cellType = CellType.WALL;
        }
        else if (Input.IsKeyPressed(Convert.ToInt32(KeyList.E)))
        {
            cellType = CellType.EMPTY;
        }
        else
        {
            return;
        }

        Vector2 paintCoords = prevMouseCoords;

        // In order to make sure that there are no breaks between appearing pixels as we draw them,
        // we divide the distance between the latest and previous mouse coordinates into a fixed
        // number of intervals and paint at the endpoint of each interval. This is not very robust, so this
        // can be instead implemented like in Sandspiel, where the length of intervals depends of the paint size.
        Vector2 step = (mouseCoords - prevMouseCoords) / 10;
        for (int i = 0; i < 10; ++i)
        {   
            sandbox.PaintCells(Convert.ToInt32(paintCoords.x), Convert.ToInt32(paintCoords.y), 2, cellType);
            paintCoords += step;
        }
    }

    public enum CellType
    {
        EMPTY,
        WALL,
        SAND,
        WATER
    }

    public struct Cell
    {
        public CellType cellType;
        public Color color;
        public byte clock;

        private static Color COLOR_SAND = new Color(1.0f, 0.82f, 0.0f, 1.0f);
        private static Color COLOR_WATER = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        private static Color COLOR_TRANSPARENT = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        private static Color COLOR_WALL = new Color(0.5f, 0.5f, 0.5f, 1.0f);

        public static Cell STATIC_WALL = new Cell(CellType.WALL);

        public Cell(CellType cellType = CellType.EMPTY, byte clock = 0)
        {
            this.cellType = cellType;
            this.color = COLOR_TRANSPARENT;
            switch (this.cellType)
            {
                case CellType.EMPTY:
                    this.color = COLOR_TRANSPARENT;
                    break;
                case CellType.SAND:
                    this.color = COLOR_SAND;
                    break;
                case CellType.WATER:
                    this.color = COLOR_WATER;
                    break;
                case CellType.WALL:
                    this.color = COLOR_WALL;
                    break;
            }
            this.clock = clock;
        }     
    }

    class Sandbox
    {
        private Image image;

        private int width;
        private int height;

        private Cell[,] cells;
        private byte gen = 0;

        public Sandbox(Image image)
        {
            this.image = image;
            width = this.image.GetWidth();
            height = this.image.GetHeight();
            cells = new Cell[this.width, this.height];

            RandomInit();
        }
        
        private void RandomInit()
        {
            for (int i = 0; i < this.width; ++i)
                for (int j = 0; j < this.height; ++j)
                {
                    uint random = GD.Randi() % 100;
                    if (random < 90)
                    {
                        cells[i,j] = new Cell();
                    }
                    else if (random < 95)
                    {
                        cells[i,j] = new Cell(CellType.WATER);
                        image.Lock();
                        image.SetPixel(i, j, cells[i,j].color);
                        image.Unlock();
                    }
                    else
                    {
                        cells[i,j] = new Cell(CellType.SAND);
                        image.Lock();
                        image.SetPixel(i, j, cells[i,j].color);
                        image.Unlock();  
                    }
                }
        }

        public Cell GetCell(int x, int y)
        {
            if (x < 0 || x > width - 1 || y < 0 || y > height - 1)
            {
                return Cell.STATIC_WALL;
            }
            else
            {
                return cells[x,y];
            }
        }

        public void SetCell(int x, int y, Cell cell)
        {
            cells[x,y] = cell;

            // I don't know what kind of overhead the Lock() and Unlock() functions have.
            // It's quite possible that it would be more optimal to call Lock() at the start
            // of each step and the Unlock() at the end. Or maybe just call Lock() once and
            // never Unlock()?
            image.Lock();
            image.SetPixel(x, y, cell.color);
            image.Unlock();
        }

        public void PaintCells(int x, int y, int diameter, CellType cellType)
        {
            int radius = Convert.ToInt32(Math.Floor(diameter / 2.0));
            for (int dx = -radius; dx < radius; ++dx)
                for (int dy = -radius; dy < radius; ++dy)
                {
                    int paintX = x + dx;
                    int paintY = y + dy;

                    if (paintX < 0 || paintX > width - 1 || paintY < 0 || paintY > height - 1)
                    {
                        continue;
                    }

                    SetCell(paintX, paintY, new Cell(cellType));
                }
        }
        
        public byte NextGen()
        {
            return Convert.ToByte((gen + 1) % 2);
        }

        // Calculates one step of the sandbox simulation and updates the image correspondingly.
        public void Process()
        {
            for (int j = 0; j < height; ++j)
                for (int i = 0; i < width; ++i)
                {
                    ref Cell cell = ref cells[i,j];

                    if (cell.clock != gen)
                        continue;

                    switch (cell.cellType)
                    {
                        case CellType.EMPTY:
                        case CellType.WALL:
                            break;
                        case CellType.SAND:
                            UpdateSand(cell, new SandApi(i, j, this));
                            break;
                        case CellType.WATER:
                            UpdateWater(cell, new SandApi(i, j, this));
                            break;
                    }
                }

            gen = NextGen();
        }

        private void UpdateSand(Cell cell, SandApi api)
        {
            int randDir = api.GetRandomDirection();
            Cell belowCell = api.GetCell(0, 1);
            if (belowCell.cellType == CellType.EMPTY)
            {
                api.SetCell(0, 1, cell);
                api.SetCell(0, 0, new Cell());
            }
            else if (api.GetCell(randDir, 1).cellType == CellType.EMPTY)
            {
                api.SetCell(randDir, 1, cell);
                api.SetCell(0, 0, new Cell());
            }
            else if (belowCell.cellType == CellType.WATER)
            {
                api.SetCell(0, 1, cell);
                api.SetCell(0, 0, belowCell);
            }
        }

        private void UpdateWater(Cell cell, SandApi api)
        {
            int randDir = api.GetRandomDirection();
            Cell belowCell = api.GetCell(0, 1);
            if (belowCell.cellType == CellType.EMPTY)
            {
                api.SetCell(0, 1, cell);
                api.SetCell(0, 0, new Cell());
            }
            else if (api.GetCell(randDir, 1).cellType == CellType.EMPTY)
            {
                api.SetCell(randDir, 1, cell);
                api.SetCell(0, 0, new Cell());
            }
            else if (api.GetCell(randDir, 0).cellType == CellType.EMPTY)
            {
                api.SetCell(randDir, 0, cell);
                api.SetCell(0, 0, new Cell());
            }
        }
    }

    // Represents the API for the cell processing functions.
    // The idea is to allow the cell processing functions to only care about the relative cell coordinates.
    // Also, Cell objects do not store their coordinates, so here we encapsulate the coordinates of the
    // currently processed cell.  
    class SandApi
    {
        private int x;
        private int y;
        private Sandbox sandbox;

        public SandApi(int x, int y, Sandbox sandbox)
        {
            this.x = x;
            this.y = y;
            this.sandbox = sandbox;
        }

        public Cell GetCell(int dx, int dy)
        {
            return sandbox.GetCell(x + dx, y + dy);
        }

        public void SetCell(int dx, int dy, Cell cell)
        {
            cell.clock = sandbox.NextGen();
            sandbox.SetCell(x + dx, y + dy, cell);
        }

        public int GetRandomDirection()
        {
            return Convert.ToInt32(GD.Randi() % 2) * 2 - 1;
        }
    }
}
