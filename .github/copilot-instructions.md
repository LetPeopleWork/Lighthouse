# Lighthouse Project - Copilot Instructions

## Project Structure

This project follows a multi-tiered architecture with:
- **Lighthouse.Backend**: C# .NET backend with RESTful APIs, controllers using dependency injection
- **Lighthouse.Frontend**: React/TypeScript frontend with component-based architecture
- **Lighthouse.EndToEndTests**: Playwright tests with page object model pattern

## Coding Styles

### C# Backend Code
- Use NUnit for tests with Assert.Multiple for grouped assertions
- Follow dependency injection patterns with interface-based design
- Use async/await for asynchronous operations
- Follow repository pattern for data access
- Mock dependencies using Moq for unit tests
- Use descriptive test method names with format: `MethodName_Scenario_ExpectedBehavior`

### TypeScript/React Frontend Code
- Use functional components with hooks
- Follow React Testing Library patterns for component tests with Vitest
- Use strong typing (TypeScript interfaces/types)
- Use double quotes for strings
- Use camelCase for variables and methods, PascalCase for components and classes
- Prefer const over let when variables aren't reassigned
- Organize component tests as `ComponentName.test.tsx` next to the implementation

### End-to-End Tests
- Use Playwright with TypeScript
- Follow Page Object Model pattern
- Use test.step for clear test organization
- Use strong typing for page objects
- Add descriptive comments for complex interactions
- Use test fixtures for setup/teardown

## Development Approach

### Test-Driven Development
- Write tests before implementing features
- Always run tests after making changes
- Keep test coverage high
- Use descriptive test names that explain the business case

### Clean Code Principles
- Create small, focused functions and methods
- Use descriptive naming (avoid abbreviations)
- Document public APIs and complex logic
- Remove unused code rather than commenting it out
- Avoid magic numbers - use named constants instead

## When Making Changes
1. For files > 300 lines, create a plan to change only what's necessary
2. Make minimal changes to achieve the goal
3. Always write or update tests when changing functionality
4. Update documentation when changing public APIs
5. Validate changes by running tests after modifications
6. Follow existing patterns in the codebase

## State Management & Services
- Use context providers for shared state
- Follow the service pattern for API communication
- Use interfaces for service contracts
- Mock services for isolated component testing

## Common Patterns
- Projects have settings and can contain features
- Teams are associated with projects and features
- Work tracking systems (Azure DevOps, Jira) integrate with the application
- Common UI pattern follows Material UI design principles
- Validation logic separates UI feedback from API calls

## Data Flow
- Follow unidirectional data flow in the React components
- Use DTOs for API request/response models
- Validate input at the API boundary
- Use proper error handling with try/catch blocks

## Specialized Task Instructions

For more detailed guidance on specific tasks, refer to these prompt files (use the `/file` command in Copilot chat to include them):

1. **React Component Development**: `.github/ReactComponent.prompt.md`
   - Use for implementing frontend components, forms, and UI features

2. **C# Backend Development**: `.github/CSharpBackend.prompt.md`
   - Use for API controllers, services, repositories, and data models

3. **Playwright End-to-End Testing**: `.github/PlaywrightTesting.prompt.md`
   - Use for creating or updating automated tests and page objects