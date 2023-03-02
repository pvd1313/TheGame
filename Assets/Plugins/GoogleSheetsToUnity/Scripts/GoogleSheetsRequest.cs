namespace GoogleSheetsToUnity
{
    public class GoogleSheetsRequest
    {
        public readonly string sheetId = "";
        public readonly string worksheetName = "Sheet1";

        public readonly string startCell = "A1";
        public readonly string endCell = "Z100";

        public readonly string titleColumn = "A";
        public readonly int titleRow = 1;

        public GoogleSheetsRequest(string sheetId, string worksheetName)
        {
            this.sheetId = sheetId;
            this.worksheetName = worksheetName;
        }

        public GoogleSheetsRequest(string sheetId, string worksheetName, string startCell)
        {
            this.sheetId = sheetId;
            this.worksheetName = worksheetName;
            this.startCell = startCell;
        }

        public GoogleSheetsRequest(string sheetId, string worksheetName, string startCell, string endCell)
        {
            this.sheetId = sheetId;
            this.worksheetName = worksheetName;
            this.startCell = startCell;
            this.endCell = endCell;
        }

        public GoogleSheetsRequest(string sheetId, string worksheetName, string startCell, string endCell, string titleColumn, int titleRow)
        {
            this.sheetId = sheetId;
            this.worksheetName = worksheetName;
            this.startCell = startCell;
            this.endCell = endCell;
            this.titleColumn = titleColumn;
            this.titleRow = titleRow;
        }
    }
}