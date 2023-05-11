using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GamesController : MonoBehaviour
{
    private enum Mode { MatchOnly, FreeMove };
    [SerializeField] private Mode mode; // два режима перемещени€, 'MatchOnly' означает, что передвинуть узел можно если произошло совпадение, иначе произойдет возврат
    [SerializeField] private float speed = 5.5f; // скорость движени€ объектов
    [SerializeField] private float destroyTimeout = .5f; // пауза в секундах, перед тем как уничтожить совпадени€
    [SerializeField] private LayerMask layerMask; // маска узла (префаба)
    [SerializeField] private Color[] color; // набор цветов/id
    [SerializeField] private int gridWidth = 7; // ширина игрового пол€
    [SerializeField] private int gridHeight = 10; // высота игрового пол€
    [SerializeField] private Node sampleObject; // образец узла (префаб)
    [SerializeField] private float sampleSize = 1; // размер узла (ширина и высота)
    private Node[,] grid;
    private Node[] nodeArray;
    private Vector3[,] position;
    private Node current, last;
    private Vector3 currentPos, lastPos;
    private List<Node> lines;
    private bool isLines, isMove, isMode;
    private float timeout;
    [SerializeField]private ScoreCounter _scoreCounter;

    void Start()
    {
        // создание игрового пол€ (2D массив) с заданными параметрами
        grid = Create2DGrid<Node>(sampleObject, gridWidth, gridHeight, sampleSize, transform);
        SetupField();
    }

    void SetupField() // стартовые установки, подготовка игрового пол€
    {
        position = new Vector3[gridWidth, gridHeight];
        nodeArray = new Node[gridWidth * gridHeight];

        int i = 0;
        int id = -1;
        int step = 0;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int j = Random.Range(0, color.Length);
                if (id != j) id = j; else step++;
                if (step > 2)
                {
                    step = 0;
                    id = (id + 1 < color.Length - 1) ? id + 1 : id - 1;
                }
                grid[x, y].ready = false;
                grid[x, y].x = x;
                grid[x, y].y = y;
                grid[x, y].id = id;
                grid[x, y].sprite.color = color[id];
                grid[x, y].gameObject.SetActive(true);
                grid[x, y].highlight.SetActive(false);
                position[x, y] = grid[x, y].transform.position;
                nodeArray[i] = grid[x, y];
                i++;
            }
        }
        current = null;
        last = null;
    }

    void DestroyLines()
    {
        if (!isLines) return;
        timeout += Time.deltaTime;
        if (timeout <= destroyTimeout) return;
        foreach (var line in lines)
        {
            _scoreCounter.AddScore();
            line.gameObject.SetActive(false);
            grid[line.x, line.y] = null;

           for (int y = line.y - 1; y >= 0 && grid[line.x, y] != null; y--)
            {
              if (!grid[line.x, y].gameObject.activeSelf)
              {
                  grid[line.x, y + 1] = grid[line.x, y];
                  grid[line.x, y] = null;
              }
            }

        }

        isMove = true;
        isLines = false;
    }


    void MoveNodes()
    {
        if (!isMove) return;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (grid[x, 0] == null)
                {
                    for (int i = 0; i < gridWidth; i++)
                        if (grid[i, 0] == null)
                            grid[i, 0] = GetFree(position[i, 0]);

                    if (nodeArray.All(node => node.gameObject.activeSelf))
                    {
                        isMove = false;
                        GridUpdate();

                        if (IsLine())
                        {
                            timeout = 0;
                            isLines = true;
                        }
                        else
                            isMode = false;
                    }
                }

                if (grid[x, y] != null && y + 1 < gridHeight && grid[x, y].gameObject.activeSelf && grid[x, y + 1] == null)
                {
                    grid[x, y].transform.position = Vector3.MoveTowards(grid[x, y].transform.position, position[x, y + 1], speed * Time.deltaTime);

                    if (grid[x, y].transform.position == position[x, y + 1])
                    {
                        grid[x, y + 1] = grid[x, y];
                        grid[x, y] = null;
                    }
                }
            }
        }
    }


    void Update()
    {
        DestroyLines();

        MoveNodes();

        if (isLines || isMove) return;

        if (last == null)
        {
            Control();
        }
        else
        {
            MoveCurrent();
        }
    }

    Node GetFree(Vector3 pos) // возвращает неактивный узел
    {
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (!nodeArray[i].gameObject.activeSelf)
            {
                int j = Random.Range(0, color.Length);
                nodeArray[i].id = j;
                nodeArray[i].sprite.color = color[j];
                nodeArray[i].transform.position = pos;
                nodeArray[i].gameObject.SetActive(true);
                return nodeArray[i];
            }
        }

        return null;
    }

    void GridUpdate() // обновление игрового пол€ с помощью рейкаста
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                RaycastHit2D hit = Physics2D.Raycast(position[x, y], Vector2.zero, Mathf.Infinity, layerMask);

                if (hit.transform != null)
                {
                    grid[x, y] = hit.transform.GetComponent<Node>();
                    grid[x, y].ready = false;
                    grid[x, y].x = x;
                    grid[x, y].y = y;
                }
            }
        }
    }

    void MoveCurrent() // перемещение выделенного мышкой узла
    {
        current.transform.position = Vector3.MoveTowards(current.transform.position, lastPos, speed * Time.deltaTime);
        last.transform.position = Vector3.MoveTowards(last.transform.position, currentPos, speed * Time.deltaTime);

        if (current.transform.position == lastPos && last.transform.position == currentPos)
        {
            GridUpdate();

            if (mode == Mode.MatchOnly && isMode && !CheckNearNodes(current) && !CheckNearNodes(last))
            {
                currentPos = position[current.x, current.y];
                lastPos = position[last.x, last.y];
                isMode = false;
                return;
            }
            else
            {
                isMode = false;
            }

            current = null;
            last = null;

            if (IsLine())
            {
                timeout = 0;
                isLines = true;
            }
        }
    }

    bool CheckNearNodes(Node node) // проверка, возможно-ли совпадение на текущем ходу
    {
        if (node.x - 2 >= 0)
            if (grid[node.x - 1, node.y].id == node.id && grid[node.x - 2, node.y].id == node.id) return true;

        if (node.y - 2 >= 0)
            if (grid[node.x, node.y - 1].id == node.id && grid[node.x, node.y - 2].id == node.id) return true;

        if (node.x + 2 < gridWidth)
            if (grid[node.x + 1, node.y].id == node.id && grid[node.x + 2, node.y].id == node.id) return true;

        if (node.y + 2 < gridHeight)
            if (grid[node.x, node.y + 1].id == node.id && grid[node.x, node.y + 2].id == node.id) return true;

        if (node.x - 1 >= 0 && node.x + 1 < gridWidth)
            if (grid[node.x - 1, node.y].id == node.id && grid[node.x + 1, node.y].id == node.id) return true;

        if (node.y - 1 >= 0 && node.y + 1 < gridHeight)
            if (grid[node.x, node.y - 1].id == node.id && grid[node.x, node.y + 1].id == node.id) return true;

        return false;
    }

    void SetNode(Node node, bool value) // метка дл€ узлов, которые наход€тс€ р€дом с выбранным (чтобы нельз€ было выбрать другие)
    {
        if (node == null) return;

        if (node.x - 1 >= 0) grid[node.x - 1, node.y].ready = value;
        if (node.y - 1 >= 0) grid[node.x, node.y - 1].ready = value;
        if (node.x + 1 < gridWidth) grid[node.x + 1, node.y].ready = value;
        if (node.y + 1 < gridHeight) grid[node.x, node.y + 1].ready = value;
    }

    void Control() // управление Ћ ћ
    {
        if (Input.GetMouseButtonDown(0) && !isMode)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, Mathf.Infinity, layerMask);

            if (hit.transform != null && current == null)
            {
                current = hit.transform.GetComponent<Node>();
                SetNode(current, true);
                current.highlight.SetActive(true);
            }
            else if (hit.transform != null && current != null)
            {
                last = hit.transform.GetComponent<Node>();

                if (last != null && !last.ready)
                {
                    current.highlight.SetActive(false);
                    last.highlight.SetActive(true);
                    SetNode(current, false);
                    SetNode(last, true);
                    current = last;
                    last = null;
                    return;
                }

                current.highlight.SetActive(false);
                currentPos = current.transform.position;
                lastPos = last.transform.position;
                isMode = true;
            }
        }
    }

    bool IsLine()
    {
        lines = new List<Node>();

        // ѕоиск совпадений по горизонтали
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 2; x++)
            {
                if (grid[x, y].id == grid[x + 1, y].id && grid[x + 1, y].id == grid[x + 2, y].id)
                {
                    lines.Add(grid[x, y]);
                    lines.Add(grid[x + 1, y]);
                    lines.Add(grid[x + 2, y]);
                }
            }
        }

        // ѕоиск совпадений по вертикали
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight - 2; y++)
            {
                if (grid[x, y].id == grid[x, y + 1].id && grid[x, y + 1].id == grid[x, y + 2].id)
                {
                    lines.Add(grid[x, y]);
                    lines.Add(grid[x, y + 1]);
                    lines.Add(grid[x, y + 2]);
                }
            }
        }

        return lines.Count > 0;
    }

    // функци€ создани€ 2D массива на основе шаблона
    private T[,] Create2DGrid<T>(T sample, int width, int height, float size, Transform parent) where T : Object
    {
        T[,] field = new T[width, height];
        Vector3 startPos = new Vector3(-size * (width - 1) / 2f, size * (height - 1) / 2f, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                field[x, y] = Instantiate(sample, startPos + new Vector3(size * x, -size * y, 0), Quaternion.identity, parent);
                field[x, y].name = "Node-" + (y * width + x);
            }
        }

        return field;
    }

}
