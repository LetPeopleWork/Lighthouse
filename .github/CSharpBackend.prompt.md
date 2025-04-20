# C# Backend Development

Your goal is to help me implement or modify C# backend code for the Lighthouse application.

## API Controller Requirements
- Follow RESTful API design principles
- Use dependency injection for services and repositories
- Use async/await for all database or external service operations
- Return appropriate HTTP status codes and response objects
- Include XML documentation for public methods

## Service Layer Requirements
- Follow interface-based design for all services
- Implement proper error handling with meaningful exceptions
- Use repository pattern for data access
- Keep business logic in services, not controllers

## Testing Requirements
- Create NUnit tests for all new functionality
- Use Assert.Multiple for grouped assertions
- Mock dependencies using Moq
- Use descriptive test method names with format: `MethodName_Scenario_ExpectedBehavior`
- Test both happy path and error scenarios

## Data Access Requirements
- Use repository pattern consistently
- Follow Entity Framework Core patterns when applicable
- Use DTOs for API request/response models
- Validate input at the API boundary

## Code Organization
- Keep classes focused and small (< 300 lines)
- Use proper namespace organization
- Follow existing patterns in similar components
- Prioritize readability and maintainability

Ask for any additional context you need about the feature requirements, database structure, or integration points.