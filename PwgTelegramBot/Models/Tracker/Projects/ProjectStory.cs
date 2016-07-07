using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{

    public class AddStoryModel
    {

        public int project_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string story_type { get; set; }
        public string current_state { get; set; }
        public float estimate { get; set; }
        public DateTime? accepted_at { get; set; }
        public DateTime? deadline { get; set; }
        public int[] owner_ids { get; set; }
        public string[] labels { get; set; } // todo: type
        public int[] label_ids { get; set; }
        public string[] tasks { get; set; } // todo: type
        public int[] follower_ids { get; set; }
        public string[] comments { get; set; } // todo: type
        public DateTime? created_at { get; set; }
        public int? before_id { get; set; }
        public int? after_id { get; set; }
        public int? integration_id { get; set; }
        public string external_id { get; set; }
    }

    public class ProjectStoryParameters
    {
        public int ProjectId { get; set; } // project_id
        public string LabelName { get; set; } // with_label
        public string Type { get; set; } // with_story_type (feature, bug, chore, release)
        public string State { get; set; } // with_state (accepted, delivered, finished, started, rejected, planned, unstarted, unscheduled)
        public int? AfterStoryId { get; set; } // after_story_id
        public int? BeforeStoryId { get; set; } // before_story_id
        public DateTime? AcceptedBefore { get; set; } // accepted_before (milliseconds)
        public DateTime? AcceptedAfter { get; set; } // accepted_after (milliseconds)
        public DateTime? CreatedBefore { get; set; } // created_before (milliseconds)
        public DateTime? CreatedAfter { get; set; } // created_after (milliseconds)
        public DateTime? UpdatedBefore { get; set; } // updated_before (milliseconds)
        public DateTime? UpdatedAfter { get; set; } // updated_after (milliseconds)
        public DateTime? DeadlineBefore { get; set; } // deadline_before (milliseconds)
        public DateTime? DeadlineAfter { get; set; } // deadline_after (milliseconds)
        public int? Limit { get; set; } // limit
        public int? Offset { get; set; } // offset
        public string Filter { get; set; } // filter (search string, can't be used with other parameters)
    }

    public class ProjectStory
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? Estimate { get; set; }
        public string StoryType { get; set; }
        public string Name { get; set; }
        public string CurrentState { get; set; }
        public int? RequestedById { get; set; }
        public string Url { get; set; }
        public int? ProjectId { get; set; }
        public List<int?> OwnerIds { get; set; }
        public List<ProjectLabel> Labels { get; set; }
        public int? OwnedById { get; set; }

        public static ProjectStory AddStory(int projectId, string name, string description)
        {
            AddStoryModel model = new AddStoryModel();
            model.project_id = projectId;
            model.name = name;
            model.description = description;

            string postJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            dynamic json = WebRequestHelper.PostTrackerJson("https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/stories", postJson);

            ProjectStory story = JsonToStory(json);

            return story;
        }

        public static ProjectStory JsonToStory(dynamic json)
        {
            ProjectStory story = new ProjectStory();
            story.Kind = json.kind;
            story.Id = json.id;
            story.CreatedAt = json.created_at;
            story.UpdatedAt = json.updated_at;
            story.AcceptedAt = json.accepted_at;
            story.Estimate = json.estimate;
            story.StoryType = json.story_type;
            story.Name = json.name;
            story.CurrentState = json.current_state;
            story.RequestedById = json.requested_by_id;
            story.Url = json.url;
            story.ProjectId = json.project_id;

            List<int?> ownerIdsTemp = new List<int?>();
            foreach (var owner in json.owner_ids)
            {
                int ownerId = Convert.ToInt32(owner.Value);
                ownerIdsTemp.Add(ownerId);
            }
            story.OwnerIds = ownerIdsTemp;

            List<ProjectLabel> labelsTemp = new List<ProjectLabel>();
            foreach (var label in json.labels)
            {
                labelsTemp.Add(ProjectLabel.JsonToLabel(label));
            }
            story.Labels = labelsTemp;

            story.OwnedById = json.owned_by_id;

            return story;
        }

        public static List<ProjectStory> GetStories(string url)
        {
            List<ProjectStory> stories = new List<ProjectStory>();
            dynamic json = WebRequestHelper.GetTrackerJson(url);
            foreach (var element in json)
            {
                ProjectStory story = JsonToStory(element);
                stories.Add(story);
            }

            return stories;
        }

        public static List<ProjectStory> GetStories(int projectId)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/stories";
            return GetStories(url);
        }

        public static ProjectStory GetStory(int projectId, int storyId)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/stories/" + storyId;
            dynamic json = WebRequestHelper.GetTrackerJson(url);
            return JsonToStory(json);
        }

        public static List<ProjectStory> GetStories(ProjectStoryParameters parameters)
        {
            string urlParameters = "";
            if (!string.IsNullOrEmpty(parameters.LabelName) && !string.IsNullOrWhiteSpace(parameters.LabelName))
            {
                urlParameters += "&with_label=" + parameters.LabelName;
            }
            if (!string.IsNullOrEmpty(parameters.Type) && !string.IsNullOrWhiteSpace(parameters.Type))
            {
                urlParameters += "&with_story_type=" + parameters.Type;
            }
            if (!string.IsNullOrEmpty(parameters.State) && !string.IsNullOrWhiteSpace(parameters.State))
            {
                urlParameters += "&with_state=" + parameters.State;
            }
            if (parameters.AfterStoryId.HasValue)
            {
                urlParameters += "&after_story_id=" + parameters.AfterStoryId.Value;
            }
            if (parameters.BeforeStoryId.HasValue)
            {
                urlParameters += "&before_story_id=" + parameters.BeforeStoryId.Value;
            }
            if (parameters.AcceptedBefore.HasValue)
            {
                urlParameters += "&accepted_before=" + parameters.AcceptedBefore.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.AcceptedAfter.HasValue)
            {
                urlParameters += "&accepted_after=" + parameters.AcceptedAfter.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.CreatedBefore.HasValue)
            {
                urlParameters += "&created_before=" + parameters.CreatedBefore.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.CreatedAfter.HasValue)
            {
                urlParameters += "&created_after=" + parameters.CreatedAfter.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.UpdatedBefore.HasValue)
            {
                urlParameters += "&updated_before=" + parameters.UpdatedBefore.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.UpdatedAfter.HasValue)
            {
                urlParameters += "&updated_after=" + parameters.UpdatedAfter.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.DeadlineBefore.HasValue)
            {
                urlParameters += "&deadline_before=" + parameters.DeadlineBefore.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.DeadlineAfter.HasValue)
            {
                urlParameters += "&deadline_after=" + parameters.DeadlineAfter.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.Limit.HasValue)
            {
                urlParameters += "&limit=" + parameters.Limit.Value;
            }
            if (parameters.Offset.HasValue)
            {
                urlParameters += "&offset=" + parameters.Offset.Value;
            }
            if (!string.IsNullOrEmpty(parameters.Filter) && !string.IsNullOrWhiteSpace(parameters.Filter))
            {
                urlParameters = "&filter=" + parameters.Filter;
            }

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + parameters.ProjectId + "/stories?" + urlParameters;
            return GetStories(url);
        }
    }
}