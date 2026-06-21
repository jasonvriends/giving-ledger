namespace Envelope_Steward.Services
{
    public record LabelSpec(
        string DisplayName,
        int    Cols,
        int    Rows,
        float  LabelWidthIn,
        float  LabelHeightIn,
        float  ColGapIn,
        float  RowGapIn,
        float  MarginTopIn,
        float  MarginBottomIn,
        float  MarginLeftIn,
        float  MarginRightIn)
    {
        // Convert inches to QuestPDF points (1" = 72pt).
        public float LabelWPt   => LabelWidthIn   * 72f;
        public float LabelHPt   => LabelHeightIn  * 72f;
        public float ColGapPt   => ColGapIn        * 72f;
        public float RowGapPt   => RowGapIn        * 72f;
        public float MTopPt     => MarginTopIn     * 72f;
        public float MBottomPt  => MarginBottomIn  * 72f;
        public float MLeftPt    => MarginLeftIn    * 72f;
        public float MRightPt   => MarginRightIn   * 72f;

        public static readonly LabelSpec[] BuiltIn =
        [
            new("Avery 5160 — 1\" × 2-5/8\" Address (3×10, 30/page)",
                Cols: 3, Rows: 10,
                LabelWidthIn: 2.625f, LabelHeightIn: 1.0f,
                ColGapIn: 0.125f, RowGapIn: 0f,
                MarginTopIn: 0.5f, MarginBottomIn: 0.5f,
                MarginLeftIn: 0.1875f, MarginRightIn: 0.1875f),

            new("Avery 5161 — 1\" × 4\" Address (2×10, 20/page)",
                Cols: 2, Rows: 10,
                LabelWidthIn: 4.0f, LabelHeightIn: 1.0f,
                ColGapIn: 0.1875f, RowGapIn: 0f,
                MarginTopIn: 0.5f, MarginBottomIn: 0.5f,
                MarginLeftIn: 0.15625f, MarginRightIn: 0.15625f),

            new("Avery 5162 — 1-1/3\" × 4\" Address (2×7, 14/page)",
                Cols: 2, Rows: 7,
                LabelWidthIn: 4.0f, LabelHeightIn: 1.333f,
                ColGapIn: 0.1875f, RowGapIn: 0f,
                MarginTopIn: 0.833f, MarginBottomIn: 0.833f,
                MarginLeftIn: 0.15625f, MarginRightIn: 0.15625f),

            new("Avery 5163 — 2\" × 4\" Shipping (2×5, 10/page)",
                Cols: 2, Rows: 5,
                LabelWidthIn: 4.0f, LabelHeightIn: 2.0f,
                ColGapIn: 0.1875f, RowGapIn: 0f,
                MarginTopIn: 0.5f, MarginBottomIn: 0.5f,
                MarginLeftIn: 0.15625f, MarginRightIn: 0.15625f),
        ];
    }
}
