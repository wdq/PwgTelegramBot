using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectMembership
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Person Person { get; set; }
        public int ProjectId { get; set; }
        public string Role { get; set; }
        public string ProjectColor { get; set; }
        public DateTime? LastViewedAt { get; set; }
        public bool? WantsCommentNotificationEmails { get; set; }
        public bool? WillReceiveMentionNotificationsOrEmails { get; set; }

        public static ProjectMembership JsonToMembership(dynamic json)
        {
            ProjectMembership membership = new ProjectMembership();
            membership.Kind = json.kind;
            membership.Id = json.id;
            membership.CreatedAt = json.created_at;
            membership.UpdatedAt = json.updated_at;
            membership.Person = Projects.Person.JsonToPerson(json.person);
            membership.ProjectId = json.project_id;
            membership.Role = json.role;
            membership.ProjectColor = json.project_color;
            membership.LastViewedAt = json.last_viewed_at;
            membership.WantsCommentNotificationEmails = json.wants_comment_notification_emails;
            membership.WillReceiveMentionNotificationsOrEmails = json.will_receive_mention_notifications_or_emails;

            return membership;
        }

        public static List<ProjectMembership> GetMemberships(string url, string pivotalTrackerApiToken)
        {
            List<ProjectMembership> memberships = new List<ProjectMembership>();
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                ProjectMembership membership = JsonToMembership(element);
                memberships.Add(membership);
            }

            return memberships;
        }

        public static List<ProjectMembership> GetMemberships(int projectId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/memberships";
            return GetMemberships(url, pivotalTrackerApiToken);
        }

        public static ProjectMembership GetMembership(int projectId, int membershipId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/memberships/" + membershipId;
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            return JsonToMembership(json);
        }

    }

    public class Person
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Initials { get; set; }
        public string Username { get; set; }

        public static Person JsonToPerson(dynamic json)
        {
            Person person = new Person();
            person.Kind = json.kind;
            person.Id = json.id;
            person.Name = json.name;
            person.Email = json.email;
            person.Initials = json.initials;
            person.Username = json.username;

            return person;
        }
    }
}