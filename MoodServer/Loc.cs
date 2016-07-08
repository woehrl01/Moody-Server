namespace MoodServer
{
    public class Loc
    {
        public Loc(int id, string location)
        {
            Identiefier = id;
            Location = location;
        }

        public string Location
        {
            get; set;
        }

        public int Identiefier
        {
            get; set;
        }
    }
}