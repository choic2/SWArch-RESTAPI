using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace restapi.Models
{
    public class Timecard
    {
        public Timecard(int resource)
        {
            Resource = resource;
            UniqueIdentifier = Guid.NewGuid();
            Identity = new TimecardIdentity();
            Lines = new List<AnnotatedTimecardLine>();
            Transitions = new List<Transition> { 
                new Transition(new Entered() { Resource = resource }) 
            };
        }

        public int Resource { get; private set; }
        
        [JsonProperty("id")]
        public TimecardIdentity Identity { get; private set; }

        public TimecardStatus Status { 
            get 
            { 
                return Transitions
                    .OrderByDescending(t => t.OccurredAt)
                    .First()
                    .TransitionedTo;
            } 
        }

        public DateTime Opened;

        [JsonProperty("recId")]
        public int RecordIdentity { get; set; } = 0;

        [JsonProperty("recVersion")]
        public int RecordVersion { get; set; } = 0;

        public Guid UniqueIdentifier { get; set; }

        [JsonIgnore]
        public IList<AnnotatedTimecardLine> Lines { get; set; }

        [JsonIgnore]
        public IList<Transition> Transitions { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }
    
        [JsonProperty("documentation")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "timecard-0.1";

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();

            switch (Status)
            {
                case TimecardStatus.Draft:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Submittal,
                        Relationship = ActionRelationship.Submit,
                        Reference = $"/timesheets/{Identity.Value}/submittal"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.TimesheetLine,
                        Relationship = ActionRelationship.RecordLine,
                        Reference = $"/timesheets/{Identity.Value}/lines"
                    });
                
                    break;

                case TimecardStatus.Submitted:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Rejection,
                        Relationship = ActionRelationship.Reject,
                        Reference = $"/timesheets/{Identity.Value}/rejection"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Approval,
                        Relationship = ActionRelationship.Approve,
                        Reference = $"/timesheets/{Identity.Value}/approval"
                    });

                    break;

                case TimecardStatus.Approved:
                    // terminal state, nothing possible here
                    break;

                case TimecardStatus.Cancelled:
                    // terminal state, nothing possible here
                    break;
            }

            return links;
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();

            links.Add(new DocumentLink() {
                Method = Method.Get,
                Type = ContentTypes.Transitions,
                Relationship = DocumentRelationship.Transitions,
                Reference = $"/timesheets/{Identity.Value}/transitions"
            });

            if (this.Lines.Count > 0)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.TimesheetLine,
                    Relationship = DocumentRelationship.Lines,
                    Reference = $"/timesheets/{Identity.Value}/lines"
                });
            }

            if (this.Status == TimecardStatus.Submitted)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Submittal,
                    Reference = $"/timesheets/{Identity.Value}/submittal"
                });
            }

            return links;
        }

        public AnnotatedTimecardLine AddLine(TimecardLine timecardLine)
        {
            var annotatedLine = new AnnotatedTimecardLine(timecardLine);

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        public AnnotatedTimecardLine ReplaceLine(TimecardLine timecardLine, 
                                  string lineId)
        {
            //var annotatedLine = new AnnotatedTimecardLine(timecardLine1);

            var newLine = new AnnotatedTimecardLine(timecardLine);

            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].UniqueIdentifier.ToString() == lineId)
                {
                    var lineToBeChanged = Lines[i];
                    lineToBeChanged.Week = newLine.Week;
                    lineToBeChanged.Year = newLine.Year;
                    lineToBeChanged.Day = newLine.Day;
                    lineToBeChanged.Hours = newLine.Hours;
                    lineToBeChanged.Project = newLine.Project;

                    Lines[i] = lineToBeChanged;
                    return Lines[i];
                }
            }

            return newLine;
        }

        public TimecardLine UpdateLine(TimecardLine timecardLine,
                                 string lineId)
        {
            //var annotatedLine = new AnnotatedTimecardLine(timecardLine1);

            //var newLine = new AnnotatedTimecardLine(timecardLine);
            //AnnotatedTimecardLine returnedLine = newLine;
            TimecardLine test = timecardLine;

            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].UniqueIdentifier.ToString() == lineId)
                {
                    var lineToBeChanged = Lines[i];
                    if (test.Week != 0)
                        lineToBeChanged.Week = test.Week;
                    if (test.Year > 0)
                        lineToBeChanged.Year = test.Year;
                    if (test.Day.Equals(0) || test.Day.Equals(1) || test.Day.Equals(2)
                    || test.Day.Equals(3) || test.Day.Equals(4) || test.Day.Equals(5)
                        || test.Day.Equals(5))
                        lineToBeChanged.Day = test.Day;
                    if (test.Hours >= 0)
                        lineToBeChanged.Hours = test.Hours;
                    if (!test.Project.Equals("0"))
                        lineToBeChanged.Project = test.Project;

                    Lines[i] = lineToBeChanged;
                    return lineToBeChanged;
                }
            }
            return test;
        }
    }
}