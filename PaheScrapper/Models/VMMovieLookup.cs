using System.Collections.Generic;

namespace PaheScrapper.Models
{
    public class VMMovieObject
    {
        public VMMovieObject(string url, string buttonId, string buttonName)
        {
            Url = url;
            ButtonId = buttonId;
            ButtonName = buttonName;
        }

        public string Url { get; }
        public string ButtonId { get; }
        public string ButtonName { get; }
    }

    public class VMMovieLookup
    {
        private readonly Dictionary<string, VMMovieObject> _lookup;

        public VMMovieLookup()
        {
            _lookup = new Dictionary<string, VMMovieObject>();
        }

        public void Add(string url, string buttonId, string buttonName)
        {
            _lookup.Add(buttonId, new VMMovieObject(url, buttonId, buttonName));
        }

        public void Remove(string buttonId)
        {
            _lookup.Remove(buttonId);
        }

        public bool IsVMAvailable()
        {
            return _lookup.Count > 0;
        }

        public VMMovieObject GetByButtonId(string buttonId)
        {
            return _lookup[buttonId];
        }
    }
}