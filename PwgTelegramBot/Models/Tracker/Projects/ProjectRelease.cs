using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectRelease
    {
        public string Kind { get; set; } // todo: these kind strings could probably have their own data type too
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Name { get; set; }
        public string CurrentState { get; set; } // todo: maybe a better type
        public string Url { get; set; }
        public int ProjectId { get; set; }
        public List<ProjectLabel> Labels { get; set; }

        public static ProjectRelease JsonToRelease(dynamic json)
        {
            ProjectRelease release = new ProjectRelease();
            release.Kind = json.kind;
            release.Id = json.id;
            release.CreatedAt = json.created_at;
            release.UpdatedAt = json.updated_at;
            release.Name = json.name;
            release.CurrentState = json.current_state;
            release.Url = json.url;
            release.ProjectId = json.project_id;

            List<ProjectLabel> labelsTemp = new List<ProjectLabel>();
            foreach (var labelJson in json.labels)
            {
                labelsTemp.Add(ProjectLabel.JsonToLabel(labelJson));
            }
            release.Labels = labelsTemp;

            return release;
        }

        public static List<ProjectRelease> GetReleases(string url, string pivotalTrackerApiToken)
        {
            List<ProjectRelease> releases = new List<ProjectRelease>();
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                ProjectRelease release = JsonToRelease(element);
                releases.Add(release);
            }

            return releases;
        }

        public static List<ProjectRelease> GetReleases(int projectId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases";
            return GetReleases(url, pivotalTrackerApiToken);
        }

        public static List<ProjectRelease> GetReleasesWithOffset(int projectId, int offset, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases?offset=" + offset;
            return GetReleases(url, pivotalTrackerApiToken);
        }

        public static List<ProjectRelease> GetReleasesWithLimit(int projectId, int limit, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases?limit=" + limit;
            return GetReleases(url, pivotalTrackerApiToken);
        }

        public static List<ProjectRelease> GetReleases(int projectId, int offset, int limit, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases?offset=" + offset + "&limit=" + limit;
            return GetReleases(url, pivotalTrackerApiToken);
        }

        public ProjectRelease GetRelease(int projectId, int releaseId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases/" + releaseId;
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            return JsonToRelease(json);
        }

        public static ProjectRelease GetRelease(int projectId, string releaseName, string pivotalTrackerApiToken)
        {
            int offset = 0;
            int limit = 10;

            bool loop = true;
            while (loop)
            {
                var releases = GetReleases(projectId, offset, limit, pivotalTrackerApiToken);
                if (releases.Count == 0)
                {
                    loop = false;
                }
                else
                {
                    return releases.FirstOrDefault(x => x.Name == releaseName);
                }

                offset = offset + 10;
            }

            return new ProjectRelease();
        }

        public static List<ProjectStory> GetReleaseStories(int projectId, int releaseId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/releases/" + releaseId + "/stories";
            List<ProjectStory> stories = new List<ProjectStory>();

            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                ProjectStory story = ProjectStory.JsonToStory(element);
                stories.Add(story);
            }

            return stories;
        }
    }
}