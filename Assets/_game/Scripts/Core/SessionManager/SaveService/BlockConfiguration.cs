using System.Collections.Generic;

namespace Core.SessionManager.SaveService
{
    [System.Serializable]
    public class BlockConfiguration
    {
        public string path; //путь к модолю по трансформам/парент
        public string currentGuid; // текущий гуид
        public Dictionary<string, string> setup; //свойства помеченные [PlayerProperty]
    }
}