using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PwgTelegramBot.Models.Tracker.Projects;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectEpic
    {
        public int Id { get; set; }
        public string Kind { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public ProjectLabel Label { get; set; }

        public static ProjectEpic JsonToEpic(dynamic json)
        {
            ProjectEpic epic = new ProjectEpic();
            epic.Id = json.id;
            epic.Kind = json.kind;
            epic.CreatedAt = json.created_at;
            epic.UpdatedAt = json.updated_at;
            epic.ProjectId = json.project_id;
            epic.Name = json.name;
            epic.Url = json.url;
            epic.Label = ProjectLabel.JsonToLabel(json.label);

            return epic;
        }

        public static List<ProjectEpic> GetEpics(string url)
        {
            List<ProjectEpic> epics = new List<ProjectEpic>();
            dynamic json = WebRequestHelper.GetTrackerJson(url);
            foreach (var element in json)
            {
                ProjectEpic epic = JsonToEpic(element);
                epics.Add(epic);
            }

            return epics;
        }

        public static List<ProjectEpic> GetEpics(int projectId)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/epics";
            return GetEpics(url);
        }

        public static List<ProjectEpic> GetEpics(int projectId, string filter)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/epics?filter=" + filter;
            return GetEpics(url);
        }

        public static ProjectEpic GetEpic(int projectId, int epicId)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/epics/" + epicId;
            return JsonToEpic(WebRequestHelper.GetTrackerJson(url));
        }
    }
}