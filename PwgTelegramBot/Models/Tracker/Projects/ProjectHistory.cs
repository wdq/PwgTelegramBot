using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectHistory
    {
    }

    public class TrackerProjectHistorySnapshotParameters
    {
        public int ProjectId { get; set; } // project_id
        public DateTime? StartDate { get; set; } // start_date (ISO 8601 ex. 2013-04-30)
        public DateTime? EndDate { get; set; } // end_date (ISO 8601 ex. 2013-04-30)
        public DateTime? DoneAcceptedBefore { get; set; } // done_accepted_before (milliseconds)
        public DateTime? DoneAcceptedAfter { get; set; } // done_accepted_after (milliseconds)
        public string LabelName { get; set; } // label
    }

    public class StorySnapshotCurrent
    {
        public string Kind { get;set; }
        public int StoryId { get; set; }
        public string State { get; set; }
        public DateTime? StoryAcceptedAt { get; set; }
        public int? Estimate { get; set; }
        public string StoryType { get; set; }

        public static StorySnapshotCurrent JsonToCurrent(dynamic json)
        {
            StorySnapshotCurrent current = new StorySnapshotCurrent();
            current.Kind = json.kind;
            current.StoryId = json.story_id;
            current.State = json.state;
            current.StoryAcceptedAt = json.story_accepted_at;
            current.Estimate = json.estimate;
            current.StoryType = json.story_type;

            return current;
        }
    }

    public class TrackerProjectHistorySnapshot
    {
        public string Kind { get; set; }
        public DateTime Date { get; set; }
        public List<StorySnapshotCurrent> Current { get; set; }

        public static TrackerProjectHistorySnapshot JsonToSnapshot(dynamic json)
        {
            TrackerProjectHistorySnapshot snapshot = new TrackerProjectHistorySnapshot();
            snapshot.Kind = json.kind;
            snapshot.Date = DateTime.Parse(json.date.ToString());

            List<StorySnapshotCurrent> currentsTemp = new List<StorySnapshotCurrent>();
            foreach (var element in json.current)
            {
                currentsTemp.Add(StorySnapshotCurrent.JsonToCurrent(element));
            }
            snapshot.Current = currentsTemp;

            return snapshot;
        }

        public static List<TrackerProjectHistorySnapshot> GetHistorySnapshots(string url, string pivotalTrackerApiToken)
        {
            List<TrackerProjectHistorySnapshot> snapshots = new List<TrackerProjectHistorySnapshot>();
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json.data)
            {
                TrackerProjectHistorySnapshot snapshot = JsonToSnapshot(element);
                snapshots.Add(snapshot);
            }

            return snapshots;
        }

        public static List<TrackerProjectHistorySnapshot> GetHistorySnapshots(TrackerProjectHistorySnapshotParameters parameters, string pivotalTrackerApiToken)
        {
            string urlParameters = "";
            if (parameters.StartDate.HasValue)
            {
                urlParameters += "&start_date=" + parameters.StartDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"); ;
            }
            if (parameters.EndDate.HasValue)
            {
                urlParameters += "&end_date=" + parameters.EndDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz"); ;
            }
            if (parameters.DoneAcceptedBefore.HasValue)
            {
                urlParameters += "&done_accepted_before" + parameters.DoneAcceptedBefore.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (parameters.DoneAcceptedAfter.HasValue)
            {
                urlParameters += "&done_accepted_after" + parameters.DoneAcceptedAfter.Value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            }
            if (!string.IsNullOrEmpty(parameters.LabelName) && !string.IsNullOrWhiteSpace(parameters.LabelName))
            {
                urlParameters += "&label=" + parameters.LabelName;
            }

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + parameters.ProjectId + "/history/snapshots/?" + urlParameters;
            return GetHistorySnapshots(url, pivotalTrackerApiToken);
        }
    }

    public class TrackerProjectHistoryDayParameters
    {
        public int ProjectId { get; set; } // project_id
        public string LabelName { get; set; } // label
        public DateTime? StartDate { get; set; } // start_date (ISO 8601 ex. 2013-04-30)
        public DateTime? EndDate { get; set; } // end_date (ISO 8601 ex. 2013-04-30)
    }

    public class TrackerProjectHistoryDay
    {
        public DateTime Date { get; set; }
        public double PointsAccepted { get; set; }
        public double PointsDelivered { get; set; }
        public double PointsFinished { get; set; }
        public double PointsStarted { get; set; }
        public double PointsRejected { get; set; }
        public double PointsPlanned { get; set; }
        public double PointsUnstarted { get; set; }
        public double PointsUnscheduled { get; set; }
        public double CountsAccepted { get; set; }
        public double CountsDelivered { get; set; }
        public double CountsFinished { get; set; }
        public double CountsStarted { get; set; }
        public double CountsRejected { get; set; }
        public double CountsPlanned { get; set; }
        public double CountsUnstarted { get; set; }
        public double CountsUnscheduled { get; set; }

        public static TrackerProjectHistoryDay JsonToHistoryDay(dynamic json)
        {
            TrackerProjectHistoryDay history = new TrackerProjectHistoryDay();
            history.Date = DateTime.Parse(json[0].ToString());
            history.PointsAccepted = Double.Parse(json[1].ToString());
            history.PointsDelivered = Double.Parse(json[2].ToString());
            history.PointsFinished = Double.Parse(json[3].ToString());
            history.PointsStarted = Double.Parse(json[4].ToString());
            history.PointsRejected = Double.Parse(json[5].ToString());
            history.PointsPlanned = Double.Parse(json[6].ToString());
            history.PointsUnstarted = Double.Parse(json[7].ToString());
            history.PointsUnscheduled = Double.Parse(json[8].ToString());
            history.CountsAccepted = Double.Parse(json[9].ToString());
            history.CountsDelivered = Double.Parse(json[10].ToString());
            history.CountsFinished = Double.Parse(json[11].ToString());
            history.CountsStarted = Double.Parse(json[12].ToString());
            history.CountsRejected = Double.Parse(json[13].ToString());
            history.CountsPlanned = Double.Parse(json[14].ToString());
            history.CountsUnstarted = Double.Parse(json[15].ToString());
            history.CountsUnscheduled = Double.Parse(json[16].ToString());

            return history;
        }

        public static List<TrackerProjectHistoryDay> GetHistoryDays(string url, string pivotalTrackerApiToken)
        {
            List<TrackerProjectHistoryDay> histories = new List<TrackerProjectHistoryDay>();
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json.data)
            {
                TrackerProjectHistoryDay history = JsonToHistoryDay(element);
                histories.Add(history);
            }

            return histories;
        }

        public static List<TrackerProjectHistoryDay> GetHistoryDays(TrackerProjectHistoryDayParameters parameters, string pivotalTrackerApiToken)
        {
            string urlParameters = "";
            if (!string.IsNullOrEmpty(parameters.LabelName) && !string.IsNullOrWhiteSpace(parameters.LabelName))
            {
                urlParameters += "&label=" + parameters.LabelName;
            }
            if (parameters.StartDate.HasValue)
            {
                urlParameters += "&start_date=" + parameters.StartDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
            }
            if (parameters.EndDate.HasValue)
            {
                urlParameters += "&end_date=" + parameters.EndDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
            }

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + parameters.ProjectId + "/history/days/?" + urlParameters;
            return GetHistoryDays(url, pivotalTrackerApiToken);
        }

        public static List<TrackerProjectHistoryDay> GetReleaseHistoryDays(int projectId, int releaseId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/history/releases/" + releaseId + "/days";
            return GetHistoryDays(url, pivotalTrackerApiToken);        
        }

        public static List<TrackerProjectHistoryDay> GetIterationHistoryDays(int projectId, int iterationNumber, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/history/iterations/" + iterationNumber + "/days";
            return GetHistoryDays(url, pivotalTrackerApiToken);
        }
    }
}