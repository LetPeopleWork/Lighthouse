import base from "/storage/repos/lighthouse-clients/vitest.config.ts";

// Only the suites that exercise the slice's code; the whole suite's timezone tests fail under Stryker's runner.
export default {
  ...base,
  root: "/storage/repos/lighthouse-clients",
  test: {
    ...base.test,
    include: [
      "packages/cli/src/reportListAll.test.ts",
      "packages/cli/src/reportListAllUsageData.test.ts",
      "packages/cli/src/reportCommands.test.ts",
      "packages/cli/src/usageDataOccurrences.test.ts",
      "packages/cli/src/commands.test.ts",
      "packages/client/src/reportWording.test.ts",
      "packages/mcp-core/src/reportTools.test.ts",
      "packages/mcp-core/src/runtime.test.ts",
      "packages/mcp-stdio/src/reportsAcrossOwners.test.ts",
      "packages/mcp-http/src/reports.e2e.test.ts",
    ],
  },
};
