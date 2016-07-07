using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectActivityParameters
    {
        public int ProjectId { get; set; }
        public string SortOrder { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        public DateTime OccuredBefore { get; set; }
        public DateTime OccuredAfter { get; set; }
        public int SinceVersion { get; set; }
    }

    public class ProjectActivityChangeOriginalValueCountStates
    {
        public int Accepted { get; set; }
        public int Started { get; set; }
        public int Finished { get; set; }
        public int Unstarted { get; set; }
        public int Planned { get; set; }
        public int Delivered { get; set; }
        public int Unscheduled { get; set; }
        public int Rejected { get; set; }
        public string Kind { get; set; }
    }

    public class ProjectActivityChangeOriginalValueCount
    {
        public List<ProjectActivityChangeOriginalValueCountStates> NumberOfZeroPointStoriesByState { get; set; }
        public List<ProjectActivityChangeOriginalValueCountStates> SumOfStoryEstimatesByState { get; set; }
        public List<ProjectActivityChangeOriginalValueCountStates> NumberOfStoriesByState { get; set; }
        public string Kind { get; set; }
    }

    public class ProjectActivityChangeOriginalValue
    {
        public List<ProjectActivityChangeOriginalValueCount> Counts { get; set; }
    }

    public class ProjectActivityChange
    {
        public string Kind { get; set; }
        public string ChangeType { get; set; }
        public int Id { get; set; }
        public List<ProjectActivityChangeOriginalValue> OriginalValues { get; set; }
        public List<string> NewValues { get; set; }
        public string Name { get; set; }
        // StoryType
    }

    public class ProjectActivityPerformedBy
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Initials { get; set; }
    }

    public class ProjectActivityProject
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ProjectActivityPrimaryResource
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string StoryType { get; set; }
        public string Url { get; set; }
    }

    public class ProjectActivity
    {
        public string Kind { get; set; }
        public string Guid { get; set; }
        public int ProjectVersion { get; set; }
        public string Message { get; set; }
        public string Highlight { get; set; }
        public List<ProjectActivityChange> Changes { get; set; }
        public List<ProjectActivityPrimaryResource> PrimaryResources { get; set; }
        public ProjectActivityProject Project { get; set; }
        public ProjectActivityPerformedBy PerformedBy { get; set; }
        public DateTime OccuredAt { get; set; }
    }
}