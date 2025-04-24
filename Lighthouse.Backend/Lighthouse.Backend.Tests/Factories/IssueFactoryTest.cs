using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.Factories
{
    public class IssueFactoryTest
    {
        private Mock<ILexoRankService> lexoRankServiceMock;

        private Mock<IWorkItemQueryOwner> workItemQueryOwnerMock;

        private string jsonTemplate;

        [SetUp]
        public void SetUp()
        {
            lexoRankServiceMock = new Mock<ILexoRankService>();

            workItemQueryOwnerMock = new Mock<IWorkItemQueryOwner>();

            var toDoStates = new List<string> { "Backlog", "Analysis" };
            var doingStates = new List<string> { "Implementation", "Verification" };
            var doneStates = new List<string> { "Resolved", "Closed" };

            workItemQueryOwnerMock.SetupGet(x => x.ToDoStates).Returns(toDoStates);
            workItemQueryOwnerMock.SetupGet(x => x.DoingStates).Returns(doingStates);
            workItemQueryOwnerMock.SetupGet(x => x.DoneStates).Returns(doneStates);

            SetupStateMapping(StateCategories.ToDo, toDoStates);
            SetupStateMapping(StateCategories.Doing, doingStates);
            SetupStateMapping(StateCategories.Done, doneStates);

            jsonTemplate = @"{""expand"":""renderedFields,names,schema,operations,editmeta,changelog,versionedRepresentations,customfield_10010.requestTypePractice"",""id"":""10045"",""self"":""https://letpeoplework.atlassian.net/rest/api/3/issue/10045"",""key"":""LGHTHSDMO-12"",""changelog"":{""startAt"":0,""maxResults"":100,""total"":0,""histories"": []}, ""fields"":{""statuscategorychangedate"":""2024-04-07T09:40:13.095+0200"",""issuetype"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/issuetype/10020"",""id"":""10020"",""description"":""Storiestrackfunctionalityorfeaturesexpressedasusergoals."",""iconUrl"":""https://letpeoplework.atlassian.net/rest/api/2/universal_avatar/view/type/issuetype/avatar/10315?size=medium"",""name"":""Story"",""subtask"":false,""avatarId"":10315,""entityId"":""89fa7ce1-7c35-46bd-8e22-e67215414703"",""hierarchyLevel"":0},""parent"":{""id"":""10034"",""key"":""LGHTHSDMO-1"",""self"":""https://letpeoplework.atlassian.net/rest/api/3/issue/10034"",""fields"":{""summary"":""AstralAffinitiy"",""status"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/status/10011"",""description"":"""",""iconUrl"":""https://letpeoplework.atlassian.net/"",""name"":""Backlog"",""id"":""10011"",""statusCategory"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/statuscategory/2"",""id"":2,""key"":""new"",""colorName"":""blue-gray"",""name"":""Backlog""}},""priority"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/priority/3"",""iconUrl"":""https://letpeoplework.atlassian.net/images/icons/priorities/medium.svg"",""name"":""Medium"",""id"":""3""},""issuetype"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/issuetype/10023"",""id"":""10023"",""description"":""Epicstrackcollectionsofrelatedbugs,stories,andtasks."",""iconUrl"":""https://letpeoplework.atlassian.net/rest/api/2/universal_avatar/view/type/issuetype/avatar/10307?size=medium"",""name"":""Epic"",""subtask"":false,""avatarId"":10307,""entityId"":""0c40435a-079c-4b07-b5a2-7d6f626c9db0"",""hierarchyLevel"":1}}},""timespent"":null,""project"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/project/10004"",""id"":""10004"",""key"":""LGHTHSDMO"",""name"":""LighthouseDemo"",""projectTypeKey"":""software"",""simplified"":true,""avatarUrls"":{""48x48"":""https://letpeoplework.atlassian.net/rest/api/3/universal_avatar/view/type/project/avatar/10421"",""24x24"":""https://letpeoplework.atlassian.net/rest/api/3/universal_avatar/view/type/project/avatar/10421?size=small"",""16x16"":""https://letpeoplework.atlassian.net/rest/api/3/universal_avatar/view/type/project/avatar/10421?size=xsmall"",""32x32"":""https://letpeoplework.atlassian.net/rest/api/3/universal_avatar/view/type/project/avatar/10421?size=medium""}},""customfield_10033"":null,""fixVersions"":[],""aggregatetimespent"":null,""customfield_10034"":null,""resolution"":null,""customfield_10027"":null,""customfield_10028"":null,""customfield_10029"":null,""resolutiondate"":null,""workratio"":-1,""issuerestriction"":{""issuerestrictions"":{},""shouldDisplay"":true},""lastViewed"":""2024-04-07T10:23:15.243+0200"",""watches"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/issue/LGHTHSDMO-12/watchers"",""watchCount"":1,""isWatching"":true},""created"":""2024-04-07T09:40:12.704+0200"",""customfield_10020"":[{""id"":6,""name"":""LGHTHSDMSprint1"",""state"":""future"",""boardId"":6}],""customfield_10021"":null,""customfield_10022"":null,""priority"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/priority/3"",""iconUrl"":""https://letpeoplework.atlassian.net/images/icons/priorities/medium.svg"",""name"":""Medium"",""id"":""3""},""customfield_10023"":null,""customfield_10024"":null,""customfield_10025"":null,""labels"":[""Lagunitas""],""customfield_10026"":null,""customfield_10016"":null,""customfield_10017"":null,""customfield_10018"":{""hasEpicLinkFieldDependency"":false,""showField"":false,""nonEditableReason"":{""reason"":""PLUGIN_LICENSE_ERROR"",""message"":""TheParentLinkisonlyavailabletoJiraPremiumusers.""}},""customfield_10019"":""0|i0007z:"",""timeestimate"":null,""aggregatetimeoriginalestimate"":null,""versions"":[],""issuelinks"":[],""assignee"":null,""updated"":""2024-04-07T09:43:39.658+0200"",""status"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/status/10011"",""description"":"""",""iconUrl"":""https://letpeoplework.atlassian.net/"",""name"":""Doing"",""id"":""10011"",""statusCategory"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/statuscategory/2"",""id"":2,""key"":""new"",""colorName"":""blue-gray"",""name"":""ToDo""}},""components"":[],""timeoriginalestimate"":null,""description"":null,""customfield_10010"":null,""customfield_10014"":null,""timetracking"":{},""customfield_10015"":null,""customfield_10005"":null,""customfield_10006"":null,""customfield_10007"":null,""security"":null,""customfield_10008"":null,""aggregatetimeestimate"":null,""attachment"":[],""customfield_10009"":null,""summary"":""Story 2"",""creator"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/user?accountId=712020%3A55c0ab9e-0195-4c50-9e09-8a3794eacd33"",""accountId"":""712020:55c0ab9e-0195-4c50-9e09-8a3794eacd33"",""emailAddress"":""benjhuser@gmail.com"",""avatarUrls"":{""48x48"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""24x24"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""16x16"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""32x32"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png""},""displayName"":""BenjiHuser-Berta"",""active"":true,""timeZone"":""Europe/Zurich"",""accountType"":""atlassian""},""subtasks"":[],""reporter"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/user?accountId=712020%3A55c0ab9e-0195-4c50-9e09-8a3794eacd33"",""accountId"":""712020:55c0ab9e-0195-4c50-9e09-8a3794eacd33"",""emailAddress"":""benjhuser@gmail.com"",""avatarUrls"":{""48x48"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""24x24"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""16x16"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png"",""32x32"":""https://secure.gravatar.com/avatar/c96f458d0c13b5384e7dd4d4e6fa7232?d=https%3A%2F%2Favatar-management--avatars.us-west-2.prod.public.atl-paas.net%2Finitials%2FBH-0.png""},""displayName"":""BenjiHuser-Berta"",""active"":true,""timeZone"":""Europe/Zurich"",""accountType"":""atlassian""},""aggregateprogress"":{""progress"":0,""total"":0},""customfield_10001"":null,""customfield_10002"":null,""customfield_10003"":null,""customfield_10004"":null, ""customfield_10038"":""LGHTHSDMO-1724"", ""environment"":null,""duedate"":null,""progress"":{""progress"":0,""total"":0},""comment"":{""comments"":[],""self"":""https://letpeoplework.atlassian.net/rest/api/3/issue/10045/comment"",""maxResults"":0,""total"":0,""startAt"":0},""votes"":{""self"":""https://letpeoplework.atlassian.net/rest/api/3/issue/LGHTHSDMO-12/votes"",""votes"":0,""hasVoted"":false},""worklog"":{""startAt"":0,""maxResults"":20,""total"":0,""worklogs"":[]}}}";
        }

        [Test]
        public void CreateIssue_KeyAvailable_ParsesKeyCorrect()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Key, Is.EqualTo("LGHTHSDMO-12"));
        }

        [Test]
        public void CreateIssue_NoKeyAvailable_Throws()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json.AsObject().Remove(JiraFieldNames.KeyPropertyName);
            });

            Assert.Throws<KeyNotFoundException>(() => CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object));
        }

        [Test]
        public void CreateIssue_TitleAvailable_ParsesTitleCorrect()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Title, Is.EqualTo("Story 2"));
        }

        [Test]
        public void CreateIssue_TitleNotAvailable_SetsDefaultTitle()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"].AsObject().Remove(JiraFieldNames.SummaryFieldName);
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Title, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateIssue_ParentAvailable_ParsesParentCorrect()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.ParentKey, Is.EqualTo("LGHTHSDMO-1"));
        }

        [Test]
        public void CreateIssue_ParentNotAvailable_IgnoresParent()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"].AsObject().Remove(JiraFieldNames.ParentFieldName);
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.ParentKey, Is.EqualTo(string.Empty));
        }

        [Test]
        public void CreateIssue_RankAvailable_ParsesRankCorrect()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Rank, Is.EqualTo("0|i0007z:"));
        }

        [Test]
        public void CreateIssue_RankAvailableInNonDefaultField_ParsesRankCorrect()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"].AsObject().Remove("customfield_10019");
                json["fields"].AsObject().Add("customfield_10115", "0|i0007z:");
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Rank, Is.EqualTo("0|i0007z:"));
        }

        [Test]
        public void CreateIssue_RankNotAvailable_SetsDefaultRank()
        {
            lexoRankServiceMock.Setup(x => x.Default).Returns("00000|");

            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"].AsObject().Remove("customfield_10019");
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.Rank, Is.EqualTo("00000|"));
        }

        [Test]
        public void CreateIssue_NoChangelog_SetsDefaultClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json.AsObject().Remove(JiraFieldNames.ChangelogFieldName);
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.ClosedDate, Is.Null);
        }

        [Test]
        public void CreateIssue_NoChangelog_SetsDefaultStartedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json.AsObject().Remove(JiraFieldNames.ChangelogFieldName);
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.StartedDate, Is.Null);
        }

        [Test]
        public void CreateIssue_CreatedDate_ReadsFromCorrectField()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.CreatedDate.HasValue, Is.True);
                Assert.That(issue.CreatedDate?.Date, Is.EqualTo(new DateTime(2024, 4, 7, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.CreatedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void CreateIssue_NoCreatedDate_SetsToDateTimeMinValue()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.CreatedDateFieldName] = string.Empty;
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.CreatedDate, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void CreateIssue_SetsStateCorrectly()
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.That(issue.State, Is.EqualTo("Doing"));
        }

        [Test]
        [TestCase("cf[10038]", "LGHTHSDMO-1724")]
        [TestCase("customfield_10038", "LGHTHSDMO-1724")]
        [TestCase("", "LGHTHSDMO-1")]
        public void CreateIssue_GivenAdditionalRelatedField_SetsParentCorrectly(string additionalRelatedField, string expectedParent)
        {
            var jsonDocument = CreateJsonDocument();

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object, additionalRelatedField);

            Assert.That(issue.ParentKey, Is.EqualTo(expectedParent));
        }

        [Test]
        public void CreateIssue_IsInToDoStateCategory_DoesNotSetStartedNorClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Backlog";
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.False);
                Assert.That(issue.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public void CreateIssue_IsInDoingStateCategory_SetsStartedDateButNotClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Implementation";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Backlog", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc))
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public void CreateIssue_IsInDoneStateCategory_SetsStartedDateButNotClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Resolved";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Analysis", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Resolved", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc))
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void IssueInDoing_MovedToOtherDoingState_StartedDateSetToFirstTimeItEnteredDoing()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Verification";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Analysis", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Verification", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc))
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public void IssueInDoing_MovedToToDoAndBackInDoing_StartedDateSetToSecondTimeItEnteredDoing()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Verification";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Analysis", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),  // <-- Item started
                    CreateChangelogEntry("Implementation", "Backlog", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc)),  // <-- Item moved to ToDo
                    CreateChangelogEntry("Backlog", "Verification", new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc))  // // <-- Item moved back to Doing
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public void IssueInDone_MovedToOtherDoneState_ClosedDateSetToFirstTimeItEnteredDoing()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Closed";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Backlog", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)), // <-- Item Started
                    CreateChangelogEntry("Implementation", "Verification", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Verification", "Resolved", new DateTime(2024, 9, 29, 0, 0, 0, DateTimeKind.Utc)), // <-- Item Closed
                    CreateChangelogEntry("Resolved", "Closed", new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc))
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 29, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void IssueInDone_MovedToDoingAndBackToDone_ClosedDateSetToSecondTimeItEnteredDone()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Resolved";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Backlog", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Resolved", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)), // <-- Item closed the first time
                    CreateChangelogEntry("Resolved", "Verification", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc)),  // <-- Item reopened
                    CreateChangelogEntry("Verification", "Resolved", new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc))  // <-- Item closed again
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void IssueInDone_MovedToToDoAndBackToDone_ClosedDateSetToSecondTimeItEnteredDone()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Closed";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Backlog", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Resolved", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)), // <-- Item closed the first time
                    CreateChangelogEntry("Resolved", "Analysis", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc)), // <-- Item repopened
                    CreateChangelogEntry("Analysis", "Implementation", new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Verification", new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Verification", "Resolved", new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc)), // <-- Item closed again
                    CreateChangelogEntry("Resolved", "Closed", new DateTime(2024, 10, 3, 0, 0, 0, DateTimeKind.Utc)) // <-- Moved within Done states -> ignore
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 10, 2, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void IssueInUnknownState_MovedToToDo_SetsStartedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Implementation";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Removed", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public void IssueInUnknownState_MovedToDoingAndThenDone_SetsStartedAndClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Closed";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Removed", "Implementation", new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)),
                    CreateChangelogEntry("Implementation", "Closed", new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc)),
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 30, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        [Test]
        public void IssueInUnknownState_MovedToDone_SetsStartedAndClosedDate()
        {
            var jsonDocument = CreateJsonDocument(json =>
            {
                json["fields"][JiraFieldNames.StatusFieldName][JiraFieldNames.NamePropertyName] = "Closed";

                AddChangelogEntries(json, new JsonArray
                {
                    CreateChangelogEntry("Removed", "Closed", new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc)),
                });
            });

            var issue = CreateIssueFactory().CreateIssueFromJson(jsonDocument.RootElement, workItemQueryOwnerMock.Object);

            Assert.Multiple(() =>
            {
                Assert.That(issue.StartedDate.HasValue, Is.True);
                Assert.That(issue.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.StartedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(issue.ClosedDate.HasValue, Is.True);
                Assert.That(issue.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 9, 28, 0, 0, 0, DateTimeKind.Utc).Date));
                Assert.That(issue.ClosedDate?.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        private void SetupStateMapping(StateCategories stateCategory, List<string> states)
        {
            foreach (var state in states)
            {
                workItemQueryOwnerMock.Setup(x => x.MapStateToStateCategory(state)).Returns(stateCategory);
            }
        }

        private void AddChangelogEntries(JsonNode jsonNode, JsonArray changelogEntries)
        {
            var changelog = jsonNode[JiraFieldNames.ChangelogFieldName];
            changelog["total"] = changelogEntries.Count;

            changelog[JiraFieldNames.HistoriesFieldName] = changelogEntries;
        }

        private JsonObject CreateChangelogEntry(string fromState, string toState, DateTime dateTime)
        {
            var changelogEntry = new JsonObject
            {
                [JiraFieldNames.CreatedDateFieldName] = dateTime.ToString("o"),
                [JiraFieldNames.ItemsFieldName] = new JsonArray {
                    new JsonObject
                    {
                        [JiraFieldNames.FieldFieldName] = JiraFieldNames.StatusFieldName,
                        [JiraFieldNames.FromStringPropertyName] = fromState,
                        [JiraFieldNames.ToStringPropertyName] = toState
                    },
                }
            };

            return changelogEntry;
        }

        private JsonDocument CreateJsonDocument(Action<JsonNode>? modifyAction = null)
        {
            var jsonNode = JsonNode.Parse(jsonTemplate) ?? throw new InvalidOperationException("Failed to parse JSON template.");
            modifyAction?.Invoke(jsonNode);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            jsonNode.WriteTo(writer);
            writer.Flush();

            return JsonDocument.Parse(stream.ToArray());
        }

        private IssueFactory CreateIssueFactory()
        {
            return new IssueFactory(lexoRankServiceMock.Object, Mock.Of<ILogger<IssueFactory>>());
        }
    }
}
