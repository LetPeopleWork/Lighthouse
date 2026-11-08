"""Which test classes talk to a real tracker, read from the C# source tree.

Those classes run only when their connector changed, so a timing figure that
counted them would move with every connector push. The scan names them so the
budget check can leave them out, and nothing else.
"""

import re
import sys
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))

import trx_to_csv

REPOSITORY = HERE.parents[2]
REAL_SOURCE_ROOT = REPOSITORY / "Lighthouse.Backend" / "Lighthouse.Backend.Tests"
BACKEND_WORKFLOW = REPOSITORY / ".github" / "workflows" / "ci_backend.yml"

NAMESPACE = "Lighthouse.Backend.Tests.Shapes"
CONNECTOR_DIRECTORY = "Services/Implementation/WorkTrackingConnectors"


def in_namespace(body, namespace=NAMESPACE):
    return f"using NUnit.Framework;\n\nnamespace {namespace}\n{{\n{body}\n}}\n"


def connector_classes(files):
    """Plant the files in a fresh tree and scan it."""
    with tempfile.TemporaryDirectory() as directory:
        root = Path(directory)
        for relative_path, content in files.items():
            path = root / relative_path
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8")
        return trx_to_csv.discover_connector_class_names(root)


def connectors_left_out_by_the_always_on_filter(workflow_text):
    """The connector categories the workflow's always-on clause leaves out."""
    clause = re.search(r'^\s*parts=\("(?P<clause>[^"]*)"', workflow_text, re.MULTILINE)
    return set(re.findall(r"Category!=(\w+)", clause.group("clause"))) - {"Integration"}


class ALiveClassIsBoundToItsOwnDeclaration(unittest.TestCase):
    def test_a_live_class_leaves_the_count_and_the_regular_class_in_its_file_stays(self):
        found = connector_classes(
            {
                "JiraConnectorTest.cs": in_namespace(
                    """
    [Category("JiraIntegration")]
    public class JiraConnectorTest
    {
        [Test]
        public void ReadsALiveTracker() { }
    }

    public class JiraHistoryCutoffTest
    {
        [Test]
        public void ReadsARecordedAnswer() { }
    }
"""
                )
            }
        )

        self.assertEqual(found, {f"{NAMESPACE}.JiraConnectorTest"})

    def test_a_comment_naming_a_connector_leaves_nobody_out(self):
        found = connector_classes(
            {
                "ServiceNowConnectorTest.cs": in_namespace(
                    """
    /// <summary>
    /// Path-scoped via Category("ServiceNowIntegration"), see the path classifier.
    /// </summary>
    // [Category("ServiceNowIntegration")]
    /* [Category("ServiceNowIntegration")] */
    public class ServiceNowConnectorTest
    {
    }
"""
                )
            }
        )

        self.assertEqual(found, set())

    def test_a_mixed_class_leaves_whole(self):
        found = connector_classes(
            {
                "JiraWriteBackTest.cs": in_namespace(
                    """
    [Category("JiraIntegration")]
    public class JiraWriteBackTest
    {
        [Test]
        [Category("Integration")]
        public void WritesToALiveTracker() { }

        [Test]
        public void BuildsTheRequestOffline() { }
    }
"""
                )
            }
        )

        self.assertEqual(found, {f"{NAMESPACE}.JiraWriteBackTest"})

    def test_a_hosted_class_in_a_connector_folder_stays_counted(self):
        found = connector_classes(
            {
                "Fixtures/IntegrationFixture.cs": in_namespace(
                    """
    [Category("Integration")]
    public abstract class IntegrationFixture
    {
    }
"""
                ),
                f"{CONNECTOR_DIRECTORY}/Jira/JiraHostedTest.cs": in_namespace(
                    """
    public class JiraHostedTest : IntegrationFixture
    {
    }
"""
                ),
            }
        )

        self.assertEqual(found, set())


class TheAttributeIsFoundHoweverTheClassIsWritten(unittest.TestCase):
    def test_the_attribute_is_found_however_the_class_is_written(self):
        shapes = {
            "another attribute between": (
                in_namespace(
                    """
    [Category("LinearIntegration")]
    [TestFixture]
    public class LiveTest
    {
    }
"""
                ),
                f"{NAMESPACE}.LiveTest",
            ),
            "a blank line between": (
                in_namespace(
                    """
    [Category("AdoIntegration")]

    public class LiveTest
    {
    }
"""
                ),
                f"{NAMESPACE}.LiveTest",
            ),
            "sealed": (
                in_namespace(
                    """
    [Category("GithubIntegration")]
    public sealed class LiveTest
    {
    }
"""
                ),
                f"{NAMESPACE}.LiveTest",
            ),
            "abstract": (
                in_namespace(
                    """
    [Category("ServiceNowIntegration")]
    public abstract class LiveTest
    {
    }
"""
                ),
                f"{NAMESPACE}.LiveTest",
            ),
            "a primary constructor": (
                in_namespace(
                    """
    [Category("JiraIntegration")]
    public class LiveTest(string tracker) : FixtureBase(tracker)
    {
    }
"""
                ),
                f"{NAMESPACE}.LiveTest",
            ),
            "a nested class": (
                in_namespace(
                    """
    public class OuterTest
    {
        [Category("AdoIntegration")]
        public class LiveTest
        {
        }
    }
"""
                ),
                f"{NAMESPACE}.OuterTest+LiveTest",
            ),
        }
        for shape, (content, expected) in shapes.items():
            with self.subTest(shape=shape):
                self.assertEqual(connector_classes({"LiveTest.cs": content}), {expected})

    def test_a_class_in_the_second_namespace_of_a_file_is_named_under_that_namespace(self):
        content = in_namespace(
            """
    public class RegularTest
    {
    }
""",
            namespace="Lighthouse.Backend.Tests.First",
        ) + in_namespace(
            """
    [Category("JiraIntegration")]
    public class LiveTest
    {
    }
""",
            namespace="Lighthouse.Backend.Tests.Second",
        )

        self.assertEqual(
            connector_classes({"TwoNamespaces.cs": content}),
            {"Lighthouse.Backend.Tests.Second.LiveTest"},
        )


class AClassBuiltOnAConnectorClassLeavesToo(unittest.TestCase):
    def test_a_class_built_on_a_connector_class_leaves_too(self):
        found = connector_classes(
            {
                "LiveBase.cs": in_namespace(
                    """
    [Category("JiraIntegration")]
    public abstract class JiraLiveBase
    {
    }

    public abstract class UnrelatedBase
    {
    }
"""
                ),
                "Derived.cs": in_namespace(
                    """
    public class OneStepTest : JiraLiveBase
    {
    }

    public abstract class MiddleBase : JiraLiveBase
    {
    }

    public class TwoStepsTest : MiddleBase
    {
    }

    public class UnrelatedTest : UnrelatedBase
    {
    }
"""
                ),
            }
        )

        self.assertEqual(
            found,
            {
                f"{NAMESPACE}.JiraLiveBase",
                f"{NAMESPACE}.OneStepTest",
                f"{NAMESPACE}.MiddleBase",
                f"{NAMESPACE}.TwoStepsTest",
            },
        )


class TheConnectorListFollowsTheWorkflow(unittest.TestCase):
    def test_the_list_matches_the_connectors_the_always_on_filter_leaves_out(self):
        workflow = BACKEND_WORKFLOW.read_text(encoding="utf-8")

        self.assertEqual(
            set(trx_to_csv.CONNECTOR_CATEGORIES),
            connectors_left_out_by_the_always_on_filter(workflow),
        )

    def test_a_sixth_connector_in_the_workflow_breaks_the_match(self):
        workflow = BACKEND_WORKFLOW.read_text(encoding="utf-8")
        planted = workflow.replace(
            "&Category!=GithubIntegration)",
            "&Category!=GithubIntegration&Category!=GitlabIntegration)",
            1,
        )

        self.assertNotEqual(planted, workflow)
        self.assertNotEqual(
            set(trx_to_csv.CONNECTOR_CATEGORIES),
            connectors_left_out_by_the_always_on_filter(planted),
        )


class TheRealTreeLeavesOutWhatItShould(unittest.TestCase):
    def test_the_real_tree_leaves_out_the_mixed_classes_and_keeps_their_regular_neighbours(self):
        found = trx_to_csv.discover_connector_class_names(REAL_SOURCE_ROOT)
        connectors = "Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors"

        for left_out in (
            f"{connectors}.Jira.JiraWriteBackTest",
            f"{connectors}.AzureDevOps.AzureDevOpsWriteBackTest",
        ):
            with self.subTest(left_out=left_out):
                self.assertIn(left_out, found)
        for counted in (
            f"{connectors}.AzureDevOps.AzureDevOpsHistoryCutoffTest",
            f"{connectors}.AzureDevOps.AzureDevOpsStateTransitionParserTest",
            f"{connectors}.Jira.JiraHistoryCutoffTest",
        ):
            with self.subTest(counted=counted):
                self.assertNotIn(counted, found)


if __name__ == "__main__":
    unittest.main()
