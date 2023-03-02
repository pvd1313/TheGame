using System;

namespace GoogleSheetsToUnity
{
    [Serializable]
    public class GoogleDataResponse
    {
        public string access_token = "";
        public string refresh_token = "";
        public string token_type = "";
        public int expires_in = 0; //just a place holder to work the the json and caculate the next refresh time
        public DateTime nextRefreshTime;
    }
}