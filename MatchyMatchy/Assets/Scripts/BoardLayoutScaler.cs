// Resizes a GridLayoutGroup so all grid cells fit evenly and remain square.

using UnityEngine;
using UnityEngine.UI;

public class BoardLayoutScaler : MonoBehaviour
{
    public GridLayoutGroup grid;
    public float innerPadding = 0f;

    public void Apply(int rows, int cols)
    {
        RectTransform rt = grid.GetComponent<RectTransform>();

        float width = rt.rect.width - innerPadding * 2f;
        float height = rt.rect.height - innerPadding * 2f;

        if (width <= 0 || height <= 0) return;

        float cellW = width / cols;
        float cellH = height / rows;

        float cellSize = Mathf.Min(cellW, cellH);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;

        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = Vector2.zero;
        grid.padding = new RectOffset(
            Mathf.RoundToInt(innerPadding),
            Mathf.RoundToInt(innerPadding),
            Mathf.RoundToInt(innerPadding),
            Mathf.RoundToInt(innerPadding)
        );
    }
}