using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class Game : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board _board;
    private CellGrid _grid;
    private bool _gameover = true;
    private bool _generated;
    private int _seed;

    private void OnValidate()
    {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        _board = GetComponentInChildren<Board>();
    }
    private void Start()
    {
        FindObjectOfType<MouseControlls>().OnSingleClick.AddListener(() =>
        {
            if(!_gameover) LocalReveal();
        });
    }
    public void Initialize(int pWidth, int pHeight, int pMines, int pSeed)
    {
        width = pWidth;
        height = pHeight;
        mineCount = pMines;
        _seed = pSeed;
    }
    public void NewGame()
    {
        StopAllCoroutines();

        UnityEngine.Random.InitState(_seed);

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        _gameover = false;
        _generated = false;

        _grid = new CellGrid(width, height);
        _board.Draw(_grid);

        if (!_generated)
        {
            _grid.GenerateMines(_grid.GetRandomCell(), mineCount);
            _grid.GenerateNumbers();
            _generated = true;
        }
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            NewGame();
            return;
        }*/

        if (!_gameover)
        {
            /*if (Input.GetMouseButtonDown(0))
            {
                LocalReveal();
            }*/
            if (Input.GetMouseButtonDown(1))
            {
                LocalFlag();
            }
            else if (Input.GetMouseButton(2))
            {
                Chord();
            }
            else if (Input.GetMouseButtonUp(2))
            {
                Unchord();
            }
        }
    }

    public void Reveal(int px, int py)
    {
        if (_grid.TryGetCell(px, py, out Cell cell))
        {
            Reveal(cell);
        }
    }

    private void LocalReveal()
    {
        if (TryGetCellAtMousePosition(out Cell cell, out int[] xy))
        {
            if (ServerBehaviour.IsThisUserServer) Reveal(cell);
            SendReveal(xy[0], xy[1]);
        }
    }
    private void Reveal(Cell cell)
    {
        if (cell.revealed) return;
        if (cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Mine:
                Explode(cell);
                break;

            case Cell.Type.Empty:
                StartCoroutine(Flood(cell));
                CheckWinCondition();
                break;

            default:
                cell.revealed = true;
                CheckWinCondition();
                break;
        }

        _board.Draw(_grid);
    }
    private void SendReveal(int x, int y)
    {
        CellNetworkContainer cont = new CellNetworkContainer(CellNetworkContainer.Instructions.Reveal);
        cont.x = x;
        cont.y = y;
        NetworkPacket packet = new NetworkPacket();
        packet.Write(cont);
        if (ServerBehaviour.IsThisUserServer) ServerBehaviour.Instance.ScheduleMessage(packet);
        else ClientBehaviour.Instance.SchedulePackage(packet);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (_gameover) yield break;
        if (cell.revealed) yield break;
        if (cell.type == Cell.Type.Mine) yield break;

        cell.revealed = true;
        _board.Draw(_grid);

        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            if (_grid.TryGetCell(cell.position.x - 1, cell.position.y, out Cell left))
            {
                StartCoroutine(Flood(left));
            }
            if (_grid.TryGetCell(cell.position.x + 1, cell.position.y, out Cell right))
            {
                StartCoroutine(Flood(right));
            }
            if (_grid.TryGetCell(cell.position.x, cell.position.y - 1, out Cell down))
            {
                StartCoroutine(Flood(down));
            }
            if (_grid.TryGetCell(cell.position.x, cell.position.y + 1, out Cell up))
            {
                StartCoroutine(Flood(up));
            }
        }
    }

    public void Flag(int px, int py)
    {
        if (_grid.TryGetCell(px, py, out Cell cell))
        {
            if (cell.revealed) return;
            cell.flagged = !cell.flagged;
            _board.Draw(_grid);
        }
    }
    private void LocalFlag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell, out int[] xy)) return;
        if (cell.revealed) return;

        CellNetworkContainer cont = new CellNetworkContainer(CellNetworkContainer.Instructions.Flag);
        cont.x = xy[0];
        cont.y = xy[1];
        NetworkPacket packet = new NetworkPacket();
        packet.Write(cont);
        if (ServerBehaviour.IsThisUserServer) Flag(cont.x, cont.y);

        if (ServerBehaviour.IsThisUserServer) ServerBehaviour.Instance.ScheduleMessage(packet);
        else ClientBehaviour.Instance.SchedulePackage(packet);


        //cell.flagged = !cell.flagged;
        //board.Draw(grid);
    }

    private void Chord()
    {
        // unchord previous cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _grid[x, y].chorded = false;
            }
        }

        // chord new cells
        if (TryGetCellAtMousePosition(out Cell chord))
        {
            for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
            {
                for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
                {
                    int x = chord.position.x + adjacentX;
                    int y = chord.position.y + adjacentY;

                    if (_grid.TryGetCell(x, y, out Cell cell))
                    {
                        cell.chorded = !cell.revealed && !cell.flagged;
                    }
                }
            }
        }

        _board.Draw(_grid);
    }

    private void Unchord()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = _grid[x, y];

                if (cell.chorded)
                {
                    Unchord(cell);
                }
            }
        }

        _board.Draw(_grid);
    }

    private void Unchord(Cell chord)
    {
        chord.chorded = false;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = chord.position.x + adjacentX;
                int y = chord.position.y + adjacentY;

                if (_grid.TryGetCell(x, y, out Cell cell))
                {
                    if (cell.revealed && cell.type == Cell.Type.Number)
                    {
                        if (_grid.CountAdjacentFlags(cell) >= cell.number)
                        {
                            Reveal(chord);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void Explode(Cell cell)
    {
        _gameover = true;

        // Set the mine as exploded
        cell.exploded = true;
        cell.revealed = true;

        // Reveal all other mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = _grid[x, y];

                if (cell.type == Cell.Type.Mine)
                {
                    cell.revealed = true;
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = _grid[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed)
                {
                    return; // no win
                }
            }
        }

        _gameover = true;

        // Flag all the mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = _grid[x, y];

                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                }
            }
        }
    }

    private bool TryGetCellAtMousePosition(out Cell cell, out int[] xy)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = _board.tilemap.WorldToCell(worldPosition);
        xy = new int[] { cellPosition.x, cellPosition.y };
        return _grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }
    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = _board.tilemap.WorldToCell(worldPosition);
        return _grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

}
