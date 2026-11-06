import os
import re
import subprocess
import sys
import tempfile
import unittest
from decimal import Decimal
from pathlib import Path

import test_check_budget as base

import check_budget  # noqa: E402

SWITCH = "LIGHTHOUSE_BUDGET_WARN_ONLY"
NS = base.TESTS_NAMESPACE
ALLOWED = base.allowed
NOT_NEEDED = "not needed"
REPOSITORY = base.HERE.parents[2]
BUDGET_STEP = "Check test-time budget"


def breach_phrase(seconds, limit, kind="budget"):
    return f"took {seconds}s, over the {limit}s {kind}"

# Limits the shipped budget file holds once it is measured on plain CI runs.
SHIPPED_FRONTEND_BUDGET = 56
SHIPPED_BACKEND_BUDGET = 34
SHIPPED_BACKEND_CEILINGS = {
    "API.Integration.PercentilesOverTime.Slice07WhereTheLimitsActuallyMovedTest": 229,
    "API.Integration.PercentilesOverTime.Slice05ReconstructCycleTimeHistoryTest": 214,
    "API.Integration.PercentilesOverTime.Slice06EveryPercentileTabSpansTheSamePeriodTest": 121,
    "Services.Implementation.Encryption.SecretCustodyServiceTests": 111,
    "API.Integration.FasterUpdates.Slice05FetchFingerprintTest": 91,
    "Services.Implementation.WorkTrackingConnectors.AzureDevOps.AzureDevOpsWorkTrackingConnectorTest": 83,
    "API.Integration.ForecastRealityCheck.Slice01OneSentenceAboutYourSamplingWindowTest": 78,
    "Services.Implementation.Encryption.PublishedKeySecretCountTests": 64,
    "Services.Implementation.Encryption.ReferencedKeyIdsTests": 57,
    "Integration.Containers.BlockedCountSnapshotMigrationTests": 53,
    "Services.Implementation.Encryption.ReadableSecretsNotOnTheActiveKeyTests": 46,
    "Integration.Containers.BlockedDurationMigrationTests": 45,
    "API.Integration.FasterUpdates.Slice03JiraCloudPortfolioDeltaTest": 40,
    "API.Integration.TeamFeatureSize.Slice01TeamFeatureSizeTest": 40,
    "API.Integration.Dependencies.Slice02DependencyDetailTest": 38,
    "API.Integration.Refinement.Slice13ReadinessTest": 37,
    "Services.Implementation.WorkTrackingConnectors.Jira.JiraWorkTrackingConnectorTest": 127,
    "Services.Implementation.WorkTrackingConnectors.ServiceNow.ServiceNowWorkTrackingConnectorIntegrationTest": 79,
    "Services.Implementation.WorkTrackingConnectors.Jira.JiraWriteBackTest": 67,
    "Services.Implementation.WorkTrackingConnectors.Jira.JiraOriginalNamesLiveTest": 524,
    "Integration.Containers.CategoryListNormalisationSeederOnBothProvidersTests": 184,
    "API.Integration.ConnectionFieldChange.Slice01FieldChangeUpdatesItsOwnersTest": 101,
    "Integration.Containers.ConnectionDefaultsUpgradeMigrationTests": 86,
}


class Workspace(base.CheckWorkspace):
    """Runs the check with the switch variable under the test's control, never the ambient one."""

    def check(self, stack, timings, *options, switch=None):
        return self._run(
            ["--allowlist", str(self.allowlist)]
            + (["--source-root", str(self.source_root)] if stack == "backend" else []),
            stack,
            timings,
            options,
            switch,
        )

    def check_shipped(self, stack, timings, switch=None):
        return self._run([], stack, timings, (), switch)

    def _run(self, files, stack, timings, options, switch):
        environment = {name: value for name, value in os.environ.items() if name != SWITCH}
        if switch is not None:
            environment[SWITCH] = switch
        command = [
            sys.executable,
            str(base.CHECK_BUDGET),
            "--stack",
            stack,
            "--csv",
            str(timings),
            *files,
            *options,
        ]
        return subprocess.run(
            command, capture_output=True, text=True, cwd=self.root, env=environment, timeout=60
        )


class TimeBudgetTestCase(base.BudgetCheckTestCase):
    def fresh_workspace(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        return Workspace(Path(folder.name))

    def breach(self, workspace, stack):
        if stack == "frontend":
            return workspace.frontend_timings({"src/pages/Overview.test.tsx": [70.0]}), "src/pages/Overview.test.tsx"
        return workspace.backend_timings({f"{NS}.CacheTest.Concurrent": 70.0}), f"{NS}.CacheTest"

    def assert_names_stale_and_warns_about_breach(self, result, stale_unit, breached_unit):
        self.assertEqual(result.returncode, 1, msg=self._explain(result))
        self.assertTrue(
            [line for line in self.annotations(result, "error") if stale_unit in line],
            msg=self._explain(result),
        )
        self.assertTrue(
            [line for line in self.annotations(result, "warning") if breached_unit in line],
            msg=self._explain(result),
        )
        self.assertEqual(
            [line for line in self.annotations(result, "error") if breached_unit in line],
            [],
            msg=self._explain(result),
        )


class TheWarnOnlySwitchIsReadFromTheEnvironment(unittest.TestCase):
    @unittest.skip("pending: warn_only_requested is not built yet")
    def test_an_unset_empty_or_false_variable_enforces(self):
        for environment in ({}, {SWITCH: ""}, {SWITCH: "false"}):
            with self.subTest(environment=environment):
                self.assertFalse(check_budget.warn_only_requested(environment, False))

    @unittest.skip("pending: warn_only_requested is not built yet")
    def test_the_variable_set_to_true_warns(self):
        self.assertTrue(check_budget.warn_only_requested({SWITCH: "true"}, False))

    @unittest.skip("pending: warn_only_requested is not built yet")
    def test_the_flag_warns_whatever_the_variable_says(self):
        for value in (None, "", "false", "true", "maybe", "TRUE"):
            environment = {} if value is None else {SWITCH: value}
            with self.subTest(environment=environment):
                self.assertTrue(check_budget.warn_only_requested(environment, True))

    @unittest.skip("pending: warn_only_requested is not built yet")
    def test_any_other_value_gives_no_verdict_and_names_the_variable(self):
        for value in ("TRUE", "True", "1", "yes", "on", " true", "false ", "maybe"):
            with self.subTest(value=value):
                with self.assertRaises(check_budget.CannotJudge) as refusal:
                    check_budget.warn_only_requested({SWITCH: value}, False)
                self.assertIn(SWITCH, str(refusal.exception))


class TheSwitchDecidesWhatABreachDoesOnBothStacks(TimeBudgetTestCase):
    def test_a_breach_fails_the_build_when_the_variable_is_unset_empty_or_false(self):
        for stack in ("frontend", "backend"):
            for switch in (None, "", "false"):
                with self.subTest(stack=stack, switch=switch):
                    workspace = self.fresh_workspace()
                    timings, unit = self.breach(workspace, stack)
                    workspace.budget()

                    self.assert_fails_naming(
                        workspace.check(stack, timings, switch=switch),
                        unit,
                        breach_phrase("70.0", 60),
                    )

    @unittest.skip("pending: the check does not read the variable yet")
    def test_a_breach_is_a_warning_and_the_build_passes_when_the_variable_is_true(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, unit = self.breach(workspace, stack)
                workspace.budget()

                self.assert_warns_and_passes(
                    workspace.check(stack, timings, switch="true"), unit, breach_phrase("70.0", 60)
                )

    def test_the_flag_wins_over_a_variable_that_is_not_valid(self):
        for stack in ("frontend", "backend"):
            for switch in ("maybe", "TRUE"):
                with self.subTest(stack=stack, switch=switch):
                    workspace = self.fresh_workspace()
                    timings, unit = self.breach(workspace, stack)
                    workspace.budget()

                    self.assert_warns_and_passes(
                        workspace.check(stack, timings, "--warn-only", switch=switch),
                        unit,
                        breach_phrase("70.0", 60),
                    )

    @unittest.skip("pending: the check does not say that enforcement is off yet")
    def test_a_notice_names_the_variable_and_says_enforcement_is_off_when_it_is_true(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, _ = self.breach(workspace, stack)
                workspace.budget()

                result = workspace.check(stack, timings, switch="true")

                self.assertTrue(
                    [
                        line
                        for line in self.annotations(result, "notice")
                        if SWITCH in line and "enforcement is off" in line
                    ],
                    msg=self._explain(result),
                )

    def test_no_notice_is_printed_while_the_budget_is_enforced(self):
        for stack in ("frontend", "backend"):
            for switch in (None, "", "false"):
                with self.subTest(stack=stack, switch=switch):
                    workspace = self.fresh_workspace()
                    timings, _ = self.breach(workspace, stack)
                    workspace.budget()

                    result = workspace.check(stack, timings, switch=switch)

                    self.assertEqual(self.annotations(result, "notice"), [], msg=self._explain(result))

    @unittest.skip("pending: the check does not read the variable yet")
    def test_any_other_value_gives_no_verdict_and_names_the_variable(self):
        for stack in ("frontend", "backend"):
            for switch in ("TRUE", "1", "yes"):
                with self.subTest(stack=stack, switch=switch):
                    workspace = self.fresh_workspace()
                    timings, _ = self.breach(workspace, stack)
                    workspace.budget()

                    self.assert_refuses_to_judge(
                        workspace.check(stack, timings, switch=switch), SWITCH
                    )

    @unittest.skip("pending: the check does not read the variable yet")
    def test_an_invalid_value_gives_no_verdict_even_when_nothing_is_over_the_budget(self):
        timings = self.workspace.backend_timings({f"{NS}.CacheTest.Reads": 1.0})
        self.workspace.budget()

        self.assert_refuses_to_judge(
            self.workspace.check("backend", timings, switch="enabled"), SWITCH
        )

    def test_a_stale_entry_still_fails_when_the_variable_is_true(self):
        timings = self.workspace.backend_timings({f"{NS}.CacheTest.Reads": 1.0})
        self.workspace.budget(backend_entries=[ALLOWED(f"{NS}.DeletedFixtureTest", 120)])

        self.assert_fails_naming(
            self.workspace.check("backend", timings, switch="true"),
            f"{NS}.DeletedFixtureTest",
            base.ALLOWLIST_NAME,
        )

    @unittest.skip("pending: the check does not read the variable yet")
    def test_a_breach_is_softened_but_a_stale_entry_in_the_same_run_still_fails(self):
        self.workspace.declare_test_class("CacheTest")
        timings = self.workspace.backend_timings({f"{NS}.CacheTest.Concurrent": 70.0})
        self.workspace.budget(backend_entries=[ALLOWED(f"{NS}.DeletedFixtureTest", 120)])

        self.assert_names_stale_and_warns_about_breach(
            self.workspace.check("backend", timings, switch="true"),
            f"{NS}.DeletedFixtureTest",
            f"{NS}.CacheTest",
        )

    def test_a_missing_or_malformed_input_still_gives_no_verdict_when_the_variable_is_true(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack, input="missing timing file"):
                workspace = self.fresh_workspace()
                workspace.budget()
                missing = workspace.root / f"test-timings-{stack}.csv"

                self.assert_refuses_to_judge(
                    workspace.check(stack, missing, switch="true"), f"test-timings-{stack}.csv"
                )
            with self.subTest(stack=stack, input="malformed allowlist"):
                workspace = self.fresh_workspace()
                timings, _ = self.breach(workspace, stack)
                workspace.raw_allowlist("not json at all")

                self.assert_refuses_to_judge(
                    workspace.check(stack, timings, switch="true"), base.ALLOWLIST_NAME
                )


class AnAllowlistEntryThatIsNotNeededFailsTheBuild(TimeBudgetTestCase):
    def entry_that_fits_the_run(self, workspace, stack, ceiling):
        if stack == "frontend":
            timings = workspace.frontend_timings({"src/pages/Heavy.test.tsx": [30.0]})
            workspace.budget(frontend_entries=[ALLOWED("src/pages/Heavy.test.tsx", ceiling)])
            return timings, "src/pages/Heavy.test.tsx"
        workspace.declare_test_class("DataProtectionKeyRingTest")
        unit = f"{NS}.DataProtectionKeyRingTest"
        timings = workspace.backend_timings({f"{unit}.SharesKeysThroughRedis": 30.0})
        workspace.budget(backend_entries=[ALLOWED(unit, ceiling)])
        return timings, unit

    @unittest.skip("pending: an entry that is not needed is not detected yet")
    def test_an_entry_whose_ceiling_equals_the_budget_fails_naming_the_entry(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, unit = self.entry_that_fits_the_run(workspace, stack, 60)

                self.assert_fails_naming(
                    workspace.check(stack, timings), unit, base.ALLOWLIST_NAME, NOT_NEEDED
                )

    def test_an_entry_whose_ceiling_is_below_the_budget_is_stricter_and_stays_allowed(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, _ = self.entry_that_fits_the_run(workspace, stack, 45)

                self.assert_passes_quietly(workspace.check(stack, timings))

    def test_an_entry_whose_ceiling_is_just_above_the_budget_passes(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, _ = self.entry_that_fits_the_run(workspace, stack, 60.1)

                self.assert_passes_quietly(workspace.check(stack, timings))

    @unittest.skip("pending: the check does not judge a frontend file's run time yet")
    def test_a_frontend_entry_whose_file_ran_under_a_quarter_of_the_budget_fails(self):
        timings = self.workspace.frontend_timings({"src/pages/Quick.test.tsx": [7.0, 6.9]})
        self.workspace.budget(
            frontend_entries=[ALLOWED("src/pages/Quick.test.tsx", 120)], frontend_budget=56
        )

        self.assert_fails_naming(
            self.workspace.check("frontend", timings),
            "src/pages/Quick.test.tsx",
            base.ALLOWLIST_NAME,
            NOT_NEEDED,
        )

    def test_a_frontend_entry_whose_file_ran_exactly_a_quarter_of_the_budget_passes(self):
        timings = self.workspace.frontend_timings({"src/pages/Heavy.test.tsx": [7.0, 7.0]})
        self.workspace.budget(
            frontend_entries=[ALLOWED("src/pages/Heavy.test.tsx", 120)], frontend_budget=56
        )

        self.assert_passes_quietly(self.workspace.check("frontend", timings))

    def test_a_backend_entry_whose_class_ran_for_no_time_or_one_test_passes(self):
        for seconds in (0.0, 0.0008, 1.0):
            with self.subTest(seconds=seconds):
                workspace = self.fresh_workspace()
                workspace.declare_test_class("JiraWriteBackTest")
                workspace.budget(backend_entries=[ALLOWED(f"{NS}.JiraWriteBackTest", 76)])
                timings = workspace.backend_timings({f"{NS}.JiraWriteBackTest.Writes": seconds})

                self.assert_passes_quietly(workspace.check("backend", timings))

    @unittest.skip("pending: an entry that is not needed is not detected yet")
    def test_an_entry_that_is_not_needed_still_fails_when_the_variable_is_true(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                workspace = self.fresh_workspace()
                timings, unit = self.entry_that_fits_the_run(workspace, stack, 60)

                self.assert_fails_naming(
                    workspace.check(stack, timings, switch="true"),
                    unit,
                    base.ALLOWLIST_NAME,
                    NOT_NEEDED,
                )

    @unittest.skip("pending: an entry that is not needed is not detected yet")
    def test_an_entry_that_is_not_needed_still_fails_when_the_flag_is_given(self):
        timings, unit = self.entry_that_fits_the_run(self.workspace, "frontend", 60)

        self.assert_fails_naming(
            self.workspace.check("frontend", timings, "--warn-only"), unit, NOT_NEEDED
        )


class UnneededEntriesAreFoundFromTheTimingsAndTheLimits(unittest.TestCase):
    @staticmethod
    def rows(stack, seconds_by_unit):
        columns = base.FRONTEND_COLUMNS if stack == "frontend" else base.BACKEND_COLUMNS
        return [
            dict(zip(columns, (unit if stack == "frontend" else f"{unit}.Runs", "t", f"{seconds * 1000:.3f}", "Passed")))
            for unit, seconds in seconds_by_unit.items()
        ]

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_only_a_ceiling_equal_to_the_budget_is_named_on_both_stacks(self):
        for stack in ("frontend", "backend"):
            with self.subTest(stack=stack):
                rows = self.rows(stack, {"A": 30.0, "B": 30.0, "C": 30.0})
                found = check_budget.unneeded_entries(
                    stack, rows, Decimal(34), {"A": Decimal("34"), "B": Decimal("33"), "C": Decimal("35")}
                )
                self.assertEqual(found, ["A"])

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_a_frontend_file_under_a_quarter_of_the_budget_is_named_and_one_at_a_quarter_is_not(self):
        rows = self.rows("frontend", {"quick": 13.9, "quarter": 14.0, "slow": 40.0})
        ceilings = {name: Decimal(120) for name in ("quick", "quarter", "slow")}

        self.assertEqual(
            check_budget.unneeded_entries("frontend", rows, Decimal(56), ceilings), ["quick"]
        )

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_a_frontend_entry_below_the_budget_is_never_named_for_running_little(self):
        rows = self.rows("frontend", {"quick": 5.0})
        self.assertEqual(
            check_budget.unneeded_entries("frontend", rows, Decimal(56), {"quick": Decimal(45)}), []
        )

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_nothing_is_named_when_every_entry_is_needed(self):
        rows = self.rows("frontend", {"slow": 40.0})
        self.assertEqual(
            check_budget.unneeded_entries("frontend", rows, Decimal(56), {"slow": Decimal(120)}), []
        )

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_a_backend_class_is_never_named_for_running_little(self):
        rows = self.rows("backend", {"Small": 0.0008})
        self.assertEqual(
            check_budget.unneeded_entries("backend", rows, Decimal(34), {"Small": Decimal(76)}), []
        )

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_an_entry_missing_from_the_run_is_left_to_the_stale_check(self):
        rows = self.rows("frontend", {"other": 40.0})
        self.assertEqual(
            check_budget.unneeded_entries("frontend", rows, Decimal(56), {"gone": Decimal(120)}), []
        )


class TheShippedBudgetFileObeysItsOwnRules(unittest.TestCase):
    def sections(self):
        return {
            stack: check_budget.load_allowlist(stack, check_budget.DEFAULT_ALLOWLIST)
            for stack in ("frontend", "backend")
        }

    def test_every_backend_entry_names_a_class_that_is_still_in_the_test_source(self):
        _, ceilings = self.sections()["backend"]
        declared = check_budget.declared_classes(check_budget.DEFAULT_BACKEND_SOURCE_ROOT)

        self.assertEqual(sorted(set(ceilings) - declared), [])

    @unittest.skip("pending: unneeded_entries is not built yet")
    def test_no_entry_is_one_the_check_would_call_not_needed_on_either_stack(self):
        for stack, (budget, ceilings) in self.sections().items():
            with self.subTest(stack=stack):
                self.assertEqual(check_budget.unneeded_entries(stack, [], budget, ceilings), [])

    @unittest.skip("pending: the budgets are not recalibrated yet")
    def test_the_frontend_budget_is_56_seconds_and_nothing_is_allowlisted(self):
        budget, ceilings = self.sections()["frontend"]

        self.assertEqual((budget, ceilings), (Decimal(SHIPPED_FRONTEND_BUDGET), {}))

    @unittest.skip("pending: the budgets are not recalibrated yet")
    def test_the_backend_budget_is_34_seconds_with_the_measured_ceilings_only(self):
        budget, ceilings = self.sections()["backend"]

        self.assertEqual(budget, Decimal(SHIPPED_BACKEND_BUDGET))
        self.assertEqual(
            ceilings,
            {
                f"Lighthouse.Backend.Tests.{unit}": Decimal(seconds)
                for unit, seconds in SHIPPED_BACKEND_CEILINGS.items()
            },
        )


class ASlowUnitPlantedInARunFailsAgainstTheShippedBudgets(TimeBudgetTestCase):
    @unittest.skip("pending: the frontend budget is not recalibrated yet")
    def test_a_frontend_file_of_57_seconds_fails_the_build(self):
        timings = self.workspace.frontend_timings({"src/pages/Planted.test.tsx": [57.0]})

        result = self.workspace.check_shipped("frontend", timings)

        self.assert_fails_naming(result, "src/pages/Planted.test.tsx", breach_phrase("57.0", 56))

    def test_a_backend_class_of_35_seconds_fails_the_build(self):
        timings = self.workspace.backend_timings({"Planted.Slow.Runs": 35.0})

        result = self.workspace.check_shipped("backend", timings)

        self.assert_fails_naming(result, "Planted.Slow", breach_phrase("35.0", 34))

    @unittest.skip("pending: the budgets are not recalibrated yet")
    def test_a_planted_backend_class_is_only_a_warning_when_the_variable_is_true(self):
        timings = self.workspace.backend_timings({"Planted.Slow.Runs": 35.0})

        result = self.workspace.check_shipped("backend", timings, switch="true")

        self.assertEqual(result.returncode, 0, msg=self._explain(result))
        self.assertTrue(
            [line for line in self.annotations(result, "warning") if "Planted.Slow" in line],
            msg=self._explain(result),
        )


class TheWorkflowStepsKeepTheSwitchOutOfTheUnitTests(unittest.TestCase):
    WORKFLOWS = {
        "backend": REPOSITORY / ".github" / "workflows" / "ci_backend.yml",
        "frontend": REPOSITORY / ".github" / "workflows" / "ci_frontend.yml",
    }

    @staticmethod
    def steps(path):
        lines = [line for line in path.read_text(encoding="utf-8").splitlines() if not line.lstrip().startswith("#")]
        return [
            step
            for step in re.split(r"(?m)^(?=\s*- name: )", "\n".join(lines))
            if step.lstrip().startswith("- name:")
        ]

    @unittest.skip("pending: the workflow steps are not split yet")
    def test_the_switch_is_in_the_environment_of_the_budget_step_and_not_of_the_unit_test_step(self):
        for stack, path in self.WORKFLOWS.items():
            with self.subTest(stack=stack):
                steps = self.steps(path)
                budget = [step for step in steps if step.lstrip().startswith(f"- name: {BUDGET_STEP}")]
                unit = [step for step in steps if "unittest discover" in step]

                self.assertEqual(len(budget), 1)
                self.assertEqual(len(unit), 1)
                self.assertNotIn("check_budget.py", unit[0])
                self.assertNotIn(SWITCH, unit[0])
                self.assertIn(f"{SWITCH}: ${{{{ vars.{SWITCH} }}}}", budget[0])
                self.assertNotIn("--warn-only", unit[0])
                self.assertNotIn("--warn-only", budget[0])


if __name__ == "__main__":
    unittest.main()
