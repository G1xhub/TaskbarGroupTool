using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace TaskbarGroupTool.Models
{
    public class TaskbarGroup
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("applications")]
        public ObservableCollection<string> Applications { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        public TaskbarGroup()
        {
            Applications = new ObservableCollection<string>();
            Id = System.Guid.NewGuid().ToString();
        }

        public TaskbarGroup(string name) : this()
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
