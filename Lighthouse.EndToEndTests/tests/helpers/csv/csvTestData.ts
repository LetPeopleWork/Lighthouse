import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";

/**
 * Helper functions to generate CSV test data with dynamic dates
 * to ensure consistent 30-day metrics for testing
 */

/**
 * Generates a CSV string for team data with dates calculated to provide consistent metrics
 * @param baseDate - The base date to calculate from (defaults to now)
 * @returns CSV string with team work items
 */
export function generateTeamCsvData(baseDate: Date = new Date()): string {
	// Calculate dates relative to the base date to ensure we have consistent last 30-day metrics
	const today = new Date(baseDate);
	const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
	const twentyDaysAgo = new Date(today.getTime() - 20 * 24 * 60 * 60 * 1000);
	const fifteenDaysAgo = new Date(today.getTime() - 15 * 24 * 60 * 60 * 1000);
	const tenDaysAgo = new Date(today.getTime() - 10 * 24 * 60 * 60 * 1000);
	const fiveDaysAgo = new Date(today.getTime() - 5 * 24 * 60 * 60 * 1000);
	const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000);

	// Format dates as YYYY-MM-DD
	const formatDate = (date: Date) => date.toISOString().split("T")[0];

	return `ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Tags,Url
ITEM-001,Shopping cart functionality,Done,User Story,${formatDate(thirtyDaysAgo)},${formatDate(twentyDaysAgo)},${formatDate(thirtyDaysAgo)},EPIC-001,frontend|ecommerce,https://system.com/item/1
ITEM-002,Product catalog display,Done,User Story,${formatDate(twentyDaysAgo)},${formatDate(fifteenDaysAgo)},${formatDate(thirtyDaysAgo)},EPIC-001,frontend|catalog,https://system.com/item/2
ITEM-003,Checkout process optimization,Done,Task,${formatDate(twentyDaysAgo)},${formatDate(fifteenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-001,frontend|checkout,
ITEM-004,Cart items not persisting,Done,Bug,${formatDate(twentyDaysAgo)},${formatDate(fifteenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-001,ecommerce|storage,
ITEM-005,User login enhancement,Done,User Story,${formatDate(twentyDaysAgo)},${formatDate(fifteenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-002,authentication|frontend,https://system.com/item/5
ITEM-006,Password reset functionality,Done,User Story,${formatDate(twentyDaysAgo)},${formatDate(tenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-002,authentication|security,https://system.com/item/6
ITEM-007,JWT token implementation,Done,Task,${formatDate(twentyDaysAgo)},${formatDate(tenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-002,backend|security,
ITEM-008,Invalid email validation error,Done,Bug,${formatDate(twentyDaysAgo)},${formatDate(tenDaysAgo)},${formatDate(twentyDaysAgo)},EPIC-002,validation|authentication,
ITEM-009,Database query optimization,Done,Task,${formatDate(fifteenDaysAgo)},${formatDate(fiveDaysAgo)},${formatDate(fifteenDaysAgo)},EPIC-003,database|performance,https://system.com/item/9
ITEM-010,API caching implementation,In Progress,User Story,${formatDate(tenDaysAgo)},,${formatDate(fifteenDaysAgo)},EPIC-003,backend|caching,
ITEM-011,Memory leak fix,In Progress,Bug,${formatDate(tenDaysAgo)},,${formatDate(tenDaysAgo)},EPIC-003,performance|memory,
ITEM-012,Load testing setup,To Do,Task,,,${formatDate(fiveDaysAgo)},EPIC-003,testing|performance,
ITEM-013,Mobile app UI components,Done,User Story,${formatDate(tenDaysAgo)},${formatDate(yesterday)},${formatDate(tenDaysAgo)},EPIC-004,mobile|ui,https://system.com/item/13
ITEM-014,API integration layer,In Progress,Task,${formatDate(fiveDaysAgo)},,${formatDate(tenDaysAgo)},EPIC-004,mobile|api,
ITEM-015,Push notification setup,To Do,User Story,,,${formatDate(fiveDaysAgo)},EPIC-004,mobile|notifications,
ITEM-016,Cross-platform compatibility,To Do,Task,,,${formatDate(fiveDaysAgo)},EPIC-004,mobile|compatibility,
ITEM-017,Application monitoring dashboard,In Progress,User Story,${formatDate(fiveDaysAgo)},,${formatDate(fiveDaysAgo)},EPIC-005,monitoring|dashboard,
ITEM-018,Alert configuration system,To Do,Task,,,${formatDate(fiveDaysAgo)},EPIC-005,monitoring|alerts,
ITEM-019,Log aggregation setup,To Do,Task,,,${formatDate(yesterday)},EPIC-005,monitoring|logging,
ITEM-020,Profile settings page,To Do,User Story,,,${formatDate(fiveDaysAgo)},EPIC-006,frontend|profile,
ITEM-021,Avatar upload functionality,To Do,Task,,,${formatDate(yesterday)},EPIC-006,frontend|upload,
ITEM-022,Payment gateway integration,To Do,User Story,,,${formatDate(yesterday)},EPIC-007,backend|payment,
ITEM-023,Transaction security validation,To Do,Task,,,${formatDate(yesterday)},EPIC-007,payment|security,`;
}

/**
 * Generates a CSV string for project data with dates calculated to provide consistent metrics
 * @param baseDate - The base date to calculate from (defaults to now)
 * @returns CSV string with project features
 */
export function generateProjectCsvData(baseDate: Date = new Date()): string {
	// Calculate dates relative to the base date to ensure we have consistent last 30-day metrics
	const today = new Date(baseDate);
	const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
	const twentyDaysAgo = new Date(today.getTime() - 20 * 24 * 60 * 60 * 1000);
	const fifteenDaysAgo = new Date(today.getTime() - 15 * 24 * 60 * 60 * 1000);
	const tenDaysAgo = new Date(today.getTime() - 10 * 24 * 60 * 60 * 1000);
	const fiveDaysAgo = new Date(today.getTime() - 5 * 24 * 60 * 60 * 1000);
	const yesterday = new Date(today.getTime() - 24 * 60 * 60 * 1000);

	// Format dates as YYYY-MM-DD
	const formatDate = (date: Date) => date.toISOString().split("T")[0];

	return `ID,Name,State,Type,Started Date,Closed Date,Owning Team,Estimated Size,Created Date,Tags,Url
EPIC-001,E-commerce Platform Enhancement,Done,Epic,${formatDate(thirtyDaysAgo)},${formatDate(fifteenDaysAgo)},Frontend Team,21,${formatDate(thirtyDaysAgo)},frontend|ecommerce,https://system.com/item/epic1
EPIC-002,User Authentication System,Done,Epic,${formatDate(twentyDaysAgo)},${formatDate(tenDaysAgo)},Backend Team,13,${formatDate(twentyDaysAgo)},backend|authentication,https://system.com/item/epic2
EPIC-003,API Performance Optimization,In Progress,Epic,${formatDate(fifteenDaysAgo)},,Backend Team,18,${formatDate(twentyDaysAgo)},backend|performance,https://system.com/item/epic3
EPIC-004,Mobile App Integration,In Progress,Epic,${formatDate(tenDaysAgo)},,Frontend Team,25,${formatDate(fifteenDaysAgo)},mobile|integration,https://system.com/item/epic4
EPIC-005,Monitoring and Alerting,In Progress,Epic,${formatDate(fiveDaysAgo)},,DevOps Team,15,${formatDate(tenDaysAgo)},monitoring|infrastructure,
EPIC-006,User Profile Management,To Do,Epic,,,Frontend Team,12,${formatDate(fiveDaysAgo)},frontend|profile,
EPIC-007,Payment Processing System,To Do,Epic,,,Backend Team,20,${formatDate(yesterday)},backend|payment,
EPIC-008,Security Audit Implementation,To Do,Epic,,,Security Team,8,${formatDate(yesterday)},security|audit,`;
}

/**
 * Creates a temporary CSV file with team data
 * @param baseDate - The base date to calculate from (defaults to now)
 * @returns Object with file path and cleanup function
 */
export function createTeamCsvFile(baseDate?: Date): {
	filePath: string;
	cleanup: () => void;
} {
	const csvData = generateTeamCsvData(baseDate);
	const tempDir = os.tmpdir();
	const fileName = `team-test-data-${Date.now()}.csv`;
	const filePath = path.join(tempDir, fileName);

	fs.writeFileSync(filePath, csvData);

	return {
		filePath,
		cleanup: () => {
			try {
				fs.unlinkSync(filePath);
			} catch (error) {
				// Ignore cleanup errors
				console.warn("Failed to cleanup test CSV file:", error);
			}
		},
	};
}

/**
 * Creates a temporary CSV file with project data
 * @param baseDate - The base date to calculate from (defaults to now)
 * @returns Object with file path and cleanup function
 */
export function createProjectCsvFile(baseDate?: Date): {
	filePath: string;
	cleanup: () => void;
} {
	const csvData = generateProjectCsvData(baseDate);
	const tempDir = os.tmpdir();
	const fileName = `project-test-data-${Date.now()}.csv`;
	const filePath = path.join(tempDir, fileName);

	fs.writeFileSync(filePath, csvData);

	return {
		filePath,
		cleanup: () => {
			try {
				fs.unlinkSync(filePath);
			} catch (error) {
				// Ignore cleanup errors
				console.warn("Failed to cleanup test CSV file:", error);
			}
		},
	};
}
