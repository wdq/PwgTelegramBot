using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class TrackerProjectIterationCycleTimeDetails
    {
        public string Kind { get; set; }
        public long? TotalCycleTime { get; set; }
        public long? StartedTime { get; set; }
        public int? StartedCount { get; set; }
        public int? FinishedTime { get; set; }
        public int? FinishedCount { get; set; }
        public long? DeliveredTime { get; set; }
        public int? DeliveredCount { get; set; }
        public long? RejectedTime { get; set; }
        public int? RejectedCount { get; set; }
        public int? StoryId { get; set; }

        public static TrackerProjectIterationCycleTimeDetails JsonToCycle(dynamic json)
        {
            TrackerProjectIterationCycleTimeDetails cycle = new TrackerProjectIterationCycleTimeDetails();
            cycle.Kind = json.kind;
            cycle.TotalCycleTime = json.total_cycle_time;
            cycle.StartedTime = json.started_time;
            cycle.StartedCount = json.started_count;
            cycle.FinishedTime = json.finished_time;
            cycle.FinishedCount = json.finished_count;
            cycle.DeliveredTime = json.delivered_time;
            cycle.DeliveredCount = json.delivered_count;
            cycle.RejectedTime = json.rejected_time;
            cycle.RejectedCount = json.rejected_count;
            cycle.StoryId = json.story_id;

            return cycle;
        }

        public static List<TrackerProjectIterationCycleTimeDetails> GetCycles(int iterationNumber, int projectId, string pivotalTrackerApiToken)
        {
            List<TrackerProjectIterationCycleTimeDetails> cycles = new List<TrackerProjectIterationCycleTimeDetails>();

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations/" + iterationNumber + "/analytics/cycle_time_details";
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var item in json)
            {
                cycles.Add(JsonToCycle(item));
            }

            return cycles;
        }
    }

    public class TrackerProjectIterationAnalytics
    {
        public string Kind { get; set; }
        public int? StoriesAccepted { get; set; }
        public int? BugsCreated { get; set; }
        public long? CycleTime { get; set; }
        public double? RejectionRate { get; set; }

        public static TrackerProjectIterationAnalytics JsonToAnalytics(dynamic json)
        {
            TrackerProjectIterationAnalytics analytics = new TrackerProjectIterationAnalytics();
            analytics.Kind = json.kind;
            analytics.StoriesAccepted = json.stories_accepted;
            analytics.BugsCreated = json.bugs_created;
            analytics.CycleTime = json.cycle_time;
            analytics.RejectionRate = json.rejection_rate;

            return analytics;
        }

        public static TrackerProjectIterationAnalytics GetAnalytics(int iterationNumber, int projectId, string pivotalTrackerApiToken)
        {
            TrackerProjectIterationAnalytics analytics = new TrackerProjectIterationAnalytics();

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations/" + iterationNumber + "/analytics";
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            analytics = JsonToAnalytics(json);

            return analytics;
        }
    }

    public class ProjectIteration
    {
        public int? Number { get; set; }
        public int? ProjectId { get; set; }
        public int? Length { get; set; }
        public int? TeamStrength { get; set; }
        public List<ProjectStory> Stories { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? Finish { get; set; }
        public string Kind { get; set; }

        public static ProjectIteration JsonToIteration(dynamic json)
        {
            ProjectIteration iteration = new ProjectIteration();
            iteration.Number = json.number;
            iteration.ProjectId = json.project_id;
            iteration.Length = json.length;
            iteration.TeamStrength = json.team_strength;

            List<ProjectStory> storiesTemp = new List<ProjectStory>();
            foreach (var story in json.stories)
            {
                storiesTemp.Add(ProjectStory.JsonToStory(story));
            }
            iteration.Stories = storiesTemp;

            iteration.Start = json.start;
            iteration.Finish = json.finish;
            iteration.Kind = json.kind;
            return iteration;
        }

        public static ProjectIteration GetIteration(int iterationNumber, int projectId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations/" + iterationNumber;
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);

            ProjectIteration iteration = JsonToIteration(json);
            
            return iteration;
        }

        private static List<ProjectIteration> GetIterationsFromUrl(string url, string pivotalTrackerApiToken)
        {
            List<ProjectIteration> iterations = new List<ProjectIteration>();

            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                ProjectIteration iteration = JsonToIteration(element);
                iterations.Add(iteration);
            }

            return iterations;
        }

        public static List<ProjectIteration> GetIterations(int projectId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations";
            List<ProjectIteration> iterations = GetIterationsFromUrl(url, pivotalTrackerApiToken);
            return iterations;
        }

        public static List<ProjectIteration> GetIterationsWithOffset(int projectId, int offset, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations?offset=" + offset;
            List<ProjectIteration> iterations = GetIterationsFromUrl(url, pivotalTrackerApiToken);
            return iterations;
        }

        public static List<ProjectIteration> GetIterationsWithLimit(int projectId, int limit, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations?limit=" + limit;
            List<ProjectIteration> iterations = GetIterationsFromUrl(url, pivotalTrackerApiToken);
            return iterations;
        }

        public static List<ProjectIteration> GetIterations(int projectId, int offset, int limit, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/iterations?offset=" + offset + "&limit=" + limit;
            List<ProjectIteration> iterations = GetIterationsFromUrl(url, pivotalTrackerApiToken);
            return iterations;
        }

    }
}