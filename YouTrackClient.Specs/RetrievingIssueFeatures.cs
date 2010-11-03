using System.Collections.Generic;
using Machine.Specifications;

namespace YouTrackClient.Specs
{
    [Subject("Retrieving Issues")]
    public class when_requesting_list_of_issues_for_project
    {
        Establish context = () =>
        {
            youtrack = new YouTrack("youtrack.jetbrains.net");
        };

        Because of = () =>
        {

            issues = youtrack.GetIssues("DCVR");
        };

        It should_return_list_of_issues_for_that_project = () =>
        {
            issues.ShouldNotBeNull();
        };

        static YouTrack youtrack;
        static IList<Issue> issues;
    }


  
}