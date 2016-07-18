using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{

    public class TrackerProjectTimeZone
    {
        public string Kind { get; set; }
        public string OlsonName { get; set; }
        public string Offset { get; set; }

        public TrackerProjectTimeZone(dynamic json)
        {
            Kind = json.kind;
            OlsonName = json.olson_name;
            Offset = json.offset;
        }
    }

    public class Project
    {

        public int Id { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public int? Version { get; set; }
        public int? IterationLength { get; set; }
        public string WeekStartDay { get; set; } // todo: maybe a better data type
        public string PointScale { get; set; } // todo: maybe a better data type
        public bool PointScaleIsCustom { get; set; }
        public bool BugsAndChoresAreEstimates { get; set; }
        public bool AutomaticPlanning { get; set; }
        public bool EnableTasks { get; set; }
        public TrackerProjectTimeZone TimeZone { get; set; }
        public int? VelocityAveragedOver { get; set; }
        public int? NumberOfDoneIterationsToShow { get; set; }
        public bool HasGoogleDomain { get; set; }
        public bool EnableIncomingEmails { get; set; }
        public int? InitialVelocity { get; set; }
        public bool Public { get; set; }
        public bool AtomEnabled { get; set; }
        public string ProjectType { get; set; } // todo: maybe a better data type
        public DateTime? StartTime { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? AccountId { get; set; }
        public int? CurrentIterationNumber { get; set; }
        public bool EnableFollowing { get; set; }

        public static Project JsonToProject(dynamic json)
        {
            Project project = new Project();
            project.Id = json.id;
            project.Kind = json.kind;
            project.Name = json.name;
            project.Version = json.version;
            project.IterationLength = json.iteration_length;
            project.WeekStartDay = json.week_start_day;
            project.PointScale = json.point_scale;
            project.PointScaleIsCustom = json.point_scale_is_custom;
            project.BugsAndChoresAreEstimates = json.bugs_and_chores_are_estimatable;
            project.AutomaticPlanning = json.automatic_planning;
            project.EnableTasks = json.enable_tasks;
            project.TimeZone = new TrackerProjectTimeZone(json.time_zone);
            project.VelocityAveragedOver = json.velocity_averaged_over;
            project.NumberOfDoneIterationsToShow = json.number_of_iterations_to_show;
            project.HasGoogleDomain = json.has_google_domain;
            project.EnableIncomingEmails = json.enable_incoming_emails;
            project.InitialVelocity = json.initial_velocity;
            project.Public = false; // todo: can't have the public as the proberty name by default
            project.AtomEnabled = json.atom_enabled;
            project.ProjectType = json.project_type;
            project.StartTime = json.start_time;
            project.CreatedAt = json.created_at;
            project.UpdatedAt = json.updated_at;
            project.AccountId = json.account_id;
            project.CurrentIterationNumber = json.current_iteration_number;
            project.EnableFollowing = json.enable_following;

            return project;            
        }

        public static Project GetProject(int projectId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId;
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);

            Project project = JsonToProject(json);

            return project;
        }

        public static Project GetProject(string projectName, string pivotalTrackerApiToken)
        {
            List<Project> allProjects = GetProjects(pivotalTrackerApiToken);
            return allProjects.FirstOrDefault(x => x.Name == projectName);
        }

        public static List<Project> GetProjects(string pivotalTrackerApiToken)
        {
            List<Project> projects = new List<Project>();

            string url = "https://www.pivotaltracker.com/services/v5/projects";
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                Project project = JsonToProject(element);
                projects.Add(project);
            }

            return projects;
        }
    }
}