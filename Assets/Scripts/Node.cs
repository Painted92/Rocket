using System.Collections;
using UnityEngine;

public class Node: MonoBehaviour
{
    public SpriteRenderer sprite; // ������ ����
    public GameObject highlight; // ������ ��������� ����
    public int id { get; set; }
    public bool ready { get; set; }
    public int x { get; set; }
    public int y { get; set; }
}