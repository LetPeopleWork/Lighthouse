# Playwright End-to-End Testing

Your goal is to help me implement or modify Playwright end-to-end tests for the Lighthouse application.

## Page Object Model Requirements
- Create strongly typed Page Object classes for all UI pages
- Follow the existing pattern of exposing both methods and element locators
- Use descriptive method names that represent user actions
- Implement proper encapsulation of page implementation details
- Provide proper constructor with page parameter

## Test Organization Requirements
- Use `test.step` for clear test organization
- Group related tests using describe blocks
- Use the appropriate test fixtures (test, testWithData, testWithUpdatedTeams)
- Follow existing patterns for test setup and teardown
- Include detailed comments for complex testing scenarios

## Test Data Requirements
- Use helper functions for test data creation
- Use the API to set up test data when possible instead of UI interactions
- Clean up test data after tests complete
- Use constants for test configuration values
- Generate random names for test entities to avoid conflicts

## Assertion Requirements
- Use appropriate expect statements with clear error messages
- Wait for elements to be visible or enabled before interacting with them
- Add appropriate timeout values for operations that might take longer
- Test both success and failure scenarios
- Verify proper state transitions and UI updates

## Best Practices
- Keep tests independent and isolated from each other
- Minimize test flakiness with proper waits and assertions
- Structure tests to be readable and maintainable
- Focus tests on user-centered scenarios
- Consider test performance and execution time

Ask for any additional context you need about the feature being tested, available page objects, or test fixtures.