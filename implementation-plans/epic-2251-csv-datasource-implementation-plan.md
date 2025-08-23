# Epic 2251: Support CSV as Datasource for Lighthouse - Implementation Plan

**Epic ID**: 2251  
**Epic Title**: Support CSV as Datasource for Lighthouse  
**Status**: In Progress  
**Created**: August 23, 2025  
**Tags**: Community, Premium  

## Overview

### Goal
Enable consultants and users to import CSV data into Lighthouse for data analysis and visualization, providing a short-term solution for users who cannot install or configure full work tracking systems.

### Context
Consultants may want to use Lighthouse for data analysis but may not be allowed to install software or may need a "business case" to get through IT security to run Lighthouse locally. This feature provides a CSV export/import capability to support this use case.

### Business Value
- Lower barrier to entry for Lighthouse adoption
- Enable data analysis for environments with IT restrictions
- Provide visualization capabilities without work tracking system integration
- Support consulting and analysis use cases

## Child Stories Analysis

### Story 3017: Extend WorkTrackingOptions with support for various methods
**Priority**: High (Foundation)  
**Effort**: Large  

**Requirements**:
- Support different data source methods (Query vs File) for different connector types
- Hide query-based UI for file-based systems (CSV only)
- Show file upload UI for file-based systems (CSV only)
- Distinguish between configurable connectors (Jira/ADO/Linear with multiple instances) and built-in connectors (CSV as single instance)

**Implementation Tasks**:
- [ ] Add `DataSourceType` enum with values: `Query`, `File`
- [ ] Add `IsConfigurableConnector` property to distinguish Jira/ADO from CSV
- [ ] Add `DataSourceType` property to `WorkTrackingSystemConnection` model
- [ ] Update `WorkTrackingSystemConnectionDto` to include data source type and configurability
- [ ] Modify `ModifyTeamSettings.tsx` for conditional field rendering based on connector type
- [ ] Modify `ModifyProjectSettings.tsx` for conditional field rendering based on connector type
- [ ] Update validation logic to check appropriate fields based on data source type
- [ ] Hide "Create Connection" buttons for non-configurable connectors (CSV only)
- [ ] Hide query fields for CSV systems, show file upload fields instead
- [ ] Keep query fields visible for Jira/ADO/Linear systems
- [ ] Implement different UI flows: Connection Selection (Jira/ADO/Linear) vs Direct Selection (CSV)

**Acceptance Criteria**:
- Query fields hidden when CSV is selected, file upload fields shown instead
- CSV appears as direct selection option without "Create Connection" workflow
- Jira/ADO/Linear require connection creation/selection before team/project setup
- Validation only applies to visible/relevant fields based on connector type
- UI clearly distinguishes between configurable and built-in connectors
- CSV selection immediately shows file upload interface

---

### Story 3018: Choose Work Tracking System Type First
**Priority**: Medium (UX Improvement)  
**Effort**: Medium  

**Requirements**:
- Change UI flow to select work tracking system type before other configuration
- Step-based approach for better user experience
- Different UI layouts based on selected system type

**Implementation Tasks**:
- [ ] Refactor team/project creation UI to use step-based approach
- [ ] Implement Step 1: Select work tracking system type
- [ ] Implement Step 2: Configure system-specific settings based on selection
- [ ] Add Material-UI Stepper component to creation workflows
- [ ] Update validation to be step-aware
- [ ] Add navigation between steps with proper validation

**Acceptance Criteria**:
- User selects work tracking system first
- Subsequent configuration options adapt to selected system
- Clear navigation between configuration steps
- Cannot proceed without valid configuration at each step

---

### Story 3019: Limit CSV Teams to 1 without Premium
**Priority**: High (Licensing)  
**Effort**: Medium  

**Requirements**:
- CSV teams limited to 1 without premium license
- Separate constraint from regular team limits (which allow 3 without premium)
- Clear messaging about limitations

**Implementation Tasks**:
- [ ] Add CSV team counting logic to licensing system
- [ ] Update `LicenseGuardAttribute` with CSV-specific team constraints
- [ ] Add `CheckCsvTeamConstraint` property to license guard
- [ ] Modify team creation/update endpoints with CSV-specific license checks
- [ ] Update `useLicenseRestrictions` hook for CSV team limits
- [ ] Add frontend tooltips and restrictions for CSV teams
- [ ] Update `TeamsController` with CSV team validation

**Acceptance Criteria**:
- Free users can create max 1 CSV team
- Premium users have unlimited CSV teams
- Clear error messages when limits exceeded
- Existing regular team limits (3) remain unchanged

---

### Story 3020: Limit CSV Projects to 0 without Premium
**Priority**: High (Licensing)  
**Effort**: Small  

**Requirements**:
- CSV projects are premium-only features
- No CSV projects allowed without premium license
- More restrictive than regular projects (which allow 1 without premium)

**Implementation Tasks**:
- [ ] Block CSV project creation entirely without premium license
- [ ] Add CSV project detection to license validation
- [ ] Update `ProjectsController` with `RequirePremium = true` for CSV projects
- [ ] Modify frontend to show premium requirement message for CSV projects
- [ ] Update `useLicenseRestrictions` to handle CSV project restrictions
- [ ] Add clear premium upgrade messaging

**Acceptance Criteria**:
- Free users cannot create any CSV projects
- Clear premium requirement messaging displayed
- Premium users can create unlimited CSV projects
- Regular project limits (1 without premium) remain unchanged

---

### Story 3021: Allow to upload file for CSV Teams as Datasource
**Priority**: High (Core Feature)  
**Effort**: Large  

**Requirements**:
- In-memory file processing (no persistent storage)
- Support common CSV formats with immediate validation
- Complete data replacement on successful import
- Use existing team creation/update APIs

**Implementation Tasks**:
- [ ] Add CSV file upload to team creation/edit UI with drag-and-drop
- [ ] Implement in-memory CSV parsing and validation with security controls
- [ ] Create CSV-to-WorkItem conversion logic using existing data models
- [ ] Integrate with existing `POST /api/teams` and `PUT /api/teams/{id}` endpoints
- [ ] Add file size limits and validation (10MB max, memory processing only)
- [ ] Handle file upload error scenarios with detailed validation feedback
- [ ] Add progress indicators for validation and import phases
- [ ] Implement security validation (file type, MIME type, CSV injection prevention)
- [ ] Add rate limiting for upload attempts (5 per user per hour)
- [ ] Create audit logging for CSV import operations
- [ ] Implement complete data replacement (delete existing + import new work items)
- [ ] Add memory management and processing timeouts (90 seconds total)

**Security Requirements**:
- File extension validation (`.csv` only, case-insensitive)
- MIME type validation (`text/csv`, `application/csv`)
- Maximum file size: 10MB (processed in memory only)
- CSV injection prevention (sanitize `=`, `+`, `-`, `@`, `\t`, `\r` prefixes)
- User authentication and team authorization required
- Rate limiting and audit logging for import operations
- Memory-only processing (no file storage required)

**Acceptance Criteria**:
- Users can upload CSV files during team creation/editing
- Drag-and-drop file upload interface with immediate validation
- File size limits enforced with clear error messages
- All-or-nothing validation - complete success or complete failure
- Successful import completely replaces existing work items
- CSV teams behave like regular teams after import

---

### Story 3022: Handle Validate for CSV
**Priority**: High (Core Feature)  
**Effort**: Large  

**Requirements**:
- Complete CSV validation (all-or-nothing approach)
- Detailed error reporting with line numbers and context
- Support standard CSV schemas with strict validation
- Integration with existing team/project validation workflows

**Implementation Tasks**:
- [ ] Define expected CSV schema for teams with strict column requirements
- [ ] Implement comprehensive CSV validation service with detailed error reporting
- [ ] Add validation for required columns: ID, Name, State, Type, StartedDate, ClosedDate
- [ ] Add validation for optional columns: CreatedDate, ParentReferenceId, Tags, Url
- [ ] Validate date formats strictly (ISO 8601 format only)
- [ ] Implement all-or-nothing validation - any error fails entire upload
- [ ] Provide detailed validation error messages with line numbers and column context
- [ ] Add frontend validation feedback display with error summary
- [ ] Implement row order inference for work item priority ordering
- [ ] Create validation error catalog with standardized error codes and messages

**Expected CSV Schema for Teams**:
```csv
ID,Name,State,Type,StartedDate,ClosedDate,CreatedDate,ParentReferenceId,Tags,Url
ITEM-001,User Story Title,In Progress,User Story,2025-01-20,2025-01-25,2025-01-15,EPIC-001,frontend|feature,https://system.com/item/1
ITEM-002,Bug Title,Done,Bug,2025-01-12,2025-01-18,2025-01-10,,bug|critical,https://system.com/item/2
ITEM-003,Database cleanup task,To Do,Task,2025-01-25,,2025-01-20,,maintenance|backend,
```

**Required Columns**:
- `ID` → Maps to `ReferenceId` (string, unique identifier)
- `Name` → Maps to `Name` (string, work item title)
- `State` → Maps to `State` (string, must map to configured team states)
- `Type` → Maps to `Type` (string, work item type like "Bug", "User Story", "Task")
- `StartedDate` → Maps to `StartedDate` (DateTime, ISO 8601 format, required for cycle time calculations)
- `ClosedDate` → Maps to `ClosedDate` (DateTime, ISO 8601 format, required for throughput tracking)

**Optional Columns**:
- `CreatedDate` → Maps to `CreatedDate` (DateTime, ISO 8601 format)
- `ParentReferenceId` → Maps to `ParentReferenceId` (string, links to parent items)
- `Tags` → Maps to `Tags` (pipe-separated string like "bug|critical|frontend")
- `Url` → Maps to `Url` (string, link back to source system)

**Row Order**: Work items will be ordered by their appearance in the CSV file (first row = highest priority/order)

**Acceptance Criteria**:
- CSV structure validation with comprehensive error reporting
- All required columns present and valid - no partial validation passes
- Date format validation strictly enforced (ISO 8601 only)
- Data type validation for all fields with clear error messages
- Detailed error reporting showing up to first 10 errors with line numbers
- Row order automatically inferred from CSV sequence for work item priority
- Validation must be 100% successful or entire upload fails

---

### Story 3023: Handle Data Loading for CSV Team
**Priority**: High (Core Feature)  
**Effort**: Large  

**Requirements**:
- Convert CSV data to standard Lighthouse work items using existing data models
- Implement no-op refresh strategy (regular updates do nothing)
- Complete data replacement approach (delete existing + import new)
- Integration with existing work item storage and state management

**Implementation Tasks**:
- [ ] Implement `CsvWorkTrackingConnector.GetWorkItemsForTeam()` as no-op (returns empty/cached results)
- [ ] Create CSV parsing logic to convert rows to standard `WorkItem` objects
- [ ] Implement state mapping from CSV states to existing state categories (Todo, Doing, Done)
- [ ] Implement complete data replacement workflow (delete all existing + import new work items)
- [ ] Use existing work item creation APIs and data storage mechanisms
- [ ] Add comprehensive error handling for malformed CSV data (fail-fast approach)
- [ ] Integrate with existing team configuration (states, work item types, etc.)
- [ ] Convert CSV dates to proper DateTime objects with timezone handling
- [ ] Implement audit logging for data replacement operations

**Data Import Strategy**:
- Complete replacement model: delete all existing work items before importing new ones
- Use existing state configuration and work item types from team settings
- Allow team settings modification after import (states, work item types, etc.)
- Map CSV states to configured state categories with user customization options

**Acceptance Criteria**:
- CSV data successfully converted to standard Lighthouse work items
- State categories properly mapped to existing team configuration
- Date fields correctly parsed and stored with proper timezone handling
- Complete data replacement works reliably (old data deleted, new data imported)
- Teams function identically to Jira/ADO teams after CSV import
- Regular refresh operations complete without errors (but change no data)
- Work items display correctly in all Lighthouse UI components

---

### Story 3024: Make sure Validate/Save works for Projects too
**Priority**: Medium (Feature Extension)  
**Effort**: Medium  

**Requirements**:
- Extend CSV functionality to projects
- Support project-specific CSV schema
- Feature-level data import
- Consistent validation and error handling

**Implementation Tasks**:
- [ ] Implement `CsvWorkTrackingConnector.GetFeaturesForProject()`
- [ ] Add project-specific CSV upload endpoint: `POST /api/projects/{id}/csv-data`
- [ ] Update `ValidateProjectSettings()` for CSV projects
- [ ] Extend file upload UI for projects
- [ ] Add project-specific CSV validation
- [ ] Define project CSV schema for features/epics
- [ ] Test end-to-end project CSV workflow
- [ ] Implement row order inference for feature ordering

**Expected CSV Schema for Projects**:
```csv
ID,Name,State,Type,StartedDate,ClosedDate,OwningTeam,EstimatedSize,CreatedDate,Tags,Url
EPIC-001,Mobile App Redesign,In Progress,Epic,2025-01-05,2025-02-15,Frontend Team,100,2025-01-01,mobile|ui|q1,https://system.com/epic/1
EPIC-002,API Performance,Done,Feature,2025-01-20,2025-02-01,Backend Team,50,2025-01-15,api|performance|backend,https://system.com/epic/2
EPIC-003,User Analytics,To Do,Epic,2025-02-01,,Data Team,75,2025-01-25,analytics|data|q2,
```

**Required Columns**:
- `ID` → Maps to `ReferenceId` (string, unique feature identifier)
- `Name` → Maps to `Name` (string, feature title/description)
- `State` → Maps to `State` (string, must map to configured project states)
- `Type` → Maps to `Type` (string, feature type like "Epic", "Feature", etc.)
- `StartedDate` → Maps to `StartedDate` (DateTime, ISO 8601 format, required for cycle time calculations)
- `ClosedDate` → Maps to `ClosedDate` (DateTime, ISO 8601 format, required for throughput tracking)

**Optional Columns**:
- `OwningTeam` → Maps to `OwningTeam` (string, team responsible for the feature)
- `EstimatedSize` → Maps to `EstimatedSize` (integer, story points or size estimate)
- `CreatedDate` → Maps to `CreatedDate` (DateTime, ISO 8601 format)
- `Tags` → Maps to `Tags` (pipe-separated string like "epic|q1|mobile")
- `Url` → Maps to `Url` (string, link back to source system)

**Row Order**: Features will be ordered by their appearance in the CSV file (first row = highest priority/order)

**Acceptance Criteria**:
- CSV upload works for projects
- Project-specific validation rules applied
- Features correctly imported and displayed
- Size estimation fields properly handled
- Feature ownership correctly assigned
- All required columns validated (ID, Name, State, Type, StartedDate, ClosedDate)
- Row order automatically inferred from CSV sequence

## Missing Requirements & Recommendations

### 1. File Management Strategy
**Gap**: Unclear file storage and retention policies  
**Priority**: Medium  
**Resolved Strategy**:
- **No Persistent Storage**: CSV files processed immediately and discarded after import
- **Memory-Only Processing**: Files validated and parsed in memory during upload
- **Complete Data Replacement**: New CSV uploads completely replace all existing work items for team/project
- **Standard Team Behavior**: After import, CSV teams behave identically to Jira/ADO teams (modifiable settings, states, etc.)
- **Manual Updates Only**: Regular refresh operations do nothing (no-op); data updates require re-upload via team edit
- **Use Existing APIs**: CSV import uses standard team/project creation endpoints - no special storage needed

### 2. Data Refresh Strategy
**Gap**: No clear approach for data updates  
**Priority**: Medium  
**Resolved Strategy**:
- **Regular Refresh = No-Op**: `CsvWorkTrackingConnector.GetWorkItemsForTeam()` returns empty/cached data (no actual refresh)
- **Manual Update Process**: Users must edit team/project settings and upload new CSV to refresh data
- **Complete Data Replacement**: New CSV upload deletes all existing work items and imports fresh data
- **No Incremental Updates**: Simpler implementation - full replacement only
- **Data Staleness Indicator**: UI shows "Last Updated" timestamp and source as "CSV Import"
- **No Conflict Resolution**: Complete replacement eliminates merge conflicts

### 3. Performance Considerations
**Gap**: No performance requirements specified  
**Priority**: High  
**Resolved Specifications**:

**File Processing Performance**:
- Maximum file size: 10MB (≈50,000 rows)
- Processing timeout: 90 seconds for validation + import
- Memory usage: Maximum 512MB per CSV processing operation
- Throughput: Process minimum 1,000 rows per second

**Upload & Validation Performance**:
- File upload timeout: 60 seconds
- Initial validation: Complete within 5 seconds
- Progress reporting: Update every 500 rows processed
- Concurrent processing: Limit 2 per user, 10 system-wide

**UI Responsiveness**:
- Progress updates: Refresh every 2 seconds during processing
- Error feedback: Display validation errors within 3 seconds
- UI blocking: No operation should block UI for more than 1 second
- Processing indicators: Show progress bar and current row being processed

**Memory Management**:
- Streaming processing: Process CSV in chunks to minimize memory footprint
- Immediate cleanup: Memory freed after processing completion/failure
- Garbage collection: Force GC after large imports to free memory

### 4. Error Handling & UX
**Gap**: Limited error handling specification  
**Priority**: Medium  
**Resolved Strategy**:

**Validation Error Handling**:
- **All-or-Nothing Validation**: CSV must be 100% valid - any error fails entire upload
- **Detailed Error Messages**: Show specific row numbers, column names, and validation failures
- **Error Message Catalog**: Standardized error codes and descriptions for common issues
- **Line-by-Line Reporting**: Display first 10 validation errors with line numbers and context

**User Experience Improvements**:
- **Sample CSV Templates**: Downloadable templates for teams and projects with example data
- **Data Preview**: Show first 10 rows of CSV before validation to confirm format
- **Progress Indicators**: Real-time progress during validation and import phases
- **Clear Success Messages**: Confirm successful import with row count and summary

**Error Recovery**:
- **No Partial Imports**: Failed validation prevents any data changes
- **Rollback Not Needed**: No data is changed until validation passes completely
- **Error State Preservation**: Keep uploaded file in memory for correction attempts
- **Retry Mechanism**: Allow immediate retry after fixing validation errors

### 5. Documentation & Training
**Gap**: No user documentation planned  
**Priority**: Low  
**Resolved Plan**:

**Technical Documentation**:
- **CSV Format Specification**: Complete schema documentation for teams and projects
- **Column Mapping Guide**: Detailed mapping from CSV columns to Lighthouse fields
- **Date Format Requirements**: ISO 8601 format examples and validation rules
- **State Mapping Instructions**: How CSV states map to Todo/Doing/Done categories

**User Guides**:
- **Step-by-Step Tutorial**: Complete workflow from CSV creation to data visualization
- **Troubleshooting Guide**: Common validation errors and how to fix them
- **Best Practices**: Recommendations for CSV data organization and preparation
- **Migration Guide**: How to export data from other tools and format for Lighthouse

**In-App Help**:
- **Tooltips and Help Text**: Contextual help throughout the CSV upload process
- **Example CSV Downloads**: Templates with sample data for immediate use
- **Validation Error Explanations**: Clear descriptions of what each error means and how to fix it
- **Video Tutorials**: Short screencasts showing the complete CSV import process

## Technical Implementation Details

### Backend Architecture

#### Connector Architecture Distinction

**Configurable Connectors (Jira, ADO, Linear)**:
- Users create multiple connection instances with different configurations
- Each connection stored as `WorkTrackingSystemConnection` record
- Require authentication, URL configuration, query setup
- Connection management UI with Add/Edit/Delete operations
- Teams/Projects reference specific connection instances
- Use query-based data retrieval from external APIs

**Built-in Connectors (CSV)**:
- Single system-wide connector instance (no connection records)
- No configuration required - always available
- No authentication or connection setup UI
- Teams/Projects directly use CSV connector without connection selection
- File upload replaces connection configuration

#### New Components
- `CsvWorkTrackingConnector` implementing `IWorkTrackingConnector` (singleton pattern, no multiple instances)
- `CsvValidationService` for comprehensive file and data validation
- `CsvParsingService` for data conversion to standard work item models
- `CsvSecurityService` for security validation and injection prevention
- `CsvImportService` for complete data replacement workflow
- `AuditLoggingService` for CSV import operation tracking

#### API Integration
- Use existing team/project creation and update endpoints
- No CSV-specific connection management endpoints required
- CSV bypasses connection creation/configuration APIs entirely
- CSV file processed during standard team/project save operations
- Integration with existing work item storage mechanisms
- No "WorkTrackingSystemConnection" records created for CSV (built-in connector)

#### Database Changes
```sql
-- Optional: Track import metadata
ALTER TABLE Teams ADD COLUMN LastCsvImportDate DATETIME NULL;
ALTER TABLE Teams ADD COLUMN CsvImportRecordCount INT NULL;

ALTER TABLE Projects ADD COLUMN LastCsvImportDate DATETIME NULL;
ALTER TABLE Projects ADD COLUMN CsvImportRecordCount INT NULL;
```

### Frontend Architecture

#### New Components
- `CsvUploadComponent` - File upload with drag-and-drop and immediate validation
- `CsvDataPreview` - Preview first 10 rows of CSV before validation
- `CsvValidationResults` - Display detailed validation errors with line numbers
- `DataSourceTypeSelector` - Choose between Query/File data sources in team/project setup

#### Modified Components
- `ModifyTeamSettings.tsx` - Add CSV file upload section (conditional on data source type)
- `ModifyProjectSettings.tsx` - Add CSV file upload section (conditional on data source type)  
- `WorkTrackingSystemComponent.tsx` - Conditional rendering for data source types
- `CreateTeamDialog.tsx` - Integrate CSV upload into standard team creation workflow
- `CreateProjectDialog.tsx` - Integrate CSV upload into standard project creation workflow

## Implementation Timeline

### Phase 1: Foundation (4 weeks)
- Stories 3016, 3017, 3018
- Basic CSV system integration
- UI flow improvements
- Data source type distinction

### Phase 2: Licensing (2 weeks)  
- Stories 3019, 3020
- CSV-specific license restrictions
- Premium feature enforcement

### Phase 3: Core Functionality (6 weeks)
- Stories 3021, 3022, 3023
- File upload and validation
- Data loading for teams
- Error handling and UX

### Phase 4: Extension (3 weeks)
- Story 3024
- Project support
- Feature parity completion

### Phase 5: Polish & Documentation (2 weeks)
- Integration testing
- User documentation
- Performance optimization
- Security review

**Total Estimated Timeline: 17 weeks**

## Testing Strategy

### Unit Tests
- CSV parsing and validation logic
- License restriction enforcement
- File upload and management
- Data conversion accuracy
- Security validation (file type, size, content)
- CSV injection prevention
- Input sanitization functions
- Rate limiting logic

### Integration Tests
- End-to-end CSV import workflow
- License boundary testing
- API endpoint functionality
- File storage and cleanup
- Authentication and authorization flows
- Security middleware integration
- Audit logging verification

### Security Tests
- File upload attack vectors (malicious files, oversized files)
- CSV injection attempts with formulas
- Path traversal attack prevention
- MIME type spoofing prevention
- Rate limiting enforcement
- Authentication bypass attempts
- SQL injection via CSV content
- XSS prevention in CSV data display

### User Acceptance Tests
- CSV upload and import scenarios
- Error handling and recovery
- Performance with large files
- Cross-browser compatibility
- Security error message clarity

## Success Criteria

### Functional Requirements
- [ ] Users can upload CSV files for teams and projects
- [ ] CSV data successfully converts to Lighthouse work items
- [ ] License restrictions properly enforced
- [ ] File validation provides clear error feedback
- [ ] Data refresh and management capabilities work correctly

### Performance Requirements
- [ ] File uploads complete within 30 seconds for files up to 10MB
- [ ] CSV processing handles up to 50,000 rows
- [ ] UI remains responsive during file operations
- [ ] Memory usage stays within acceptable limits

### Quality Requirements  
- [ ] Comprehensive error handling with user-friendly messages
- [ ] Security validation passes penetration testing
- [ ] Documentation complete and accessible
- [ ] Code coverage exceeds 80% for new components

## Risks & Mitigation

### Technical Risks
**Risk**: Large CSV files causing memory issues  
**Mitigation**: Implement streaming CSV processing and file size limits

**Risk**: CSV format variations causing parsing failures  
**Mitigation**: Robust parsing with configurable delimiters and format detection

### Business Risks
**Risk**: Feature complexity overwhelming users  
**Mitigation**: Phased rollout with comprehensive documentation and tutorials

**Risk**: Premium feature adoption concerns  
**Mitigation**: Clear value proposition and trial period for CSV projects

### Security Risks
**Risk**: Malicious file uploads  
**Mitigation**: File type validation, content scanning, and sandboxed processing

## Future Enhancements

### Version 2 Features
- Multiple file format support (Excel, JSON)
- Advanced CSV mapping configuration
- Automated data refresh from file shares
- Real-time collaboration on uploaded data
- Data validation rules customization

### Integration Opportunities  
- Export to CSV from existing work tracking systems
- Integration with cloud storage providers
- Automated data pipeline configurations
- Advanced analytics on imported data

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|---------|
| 2025-08-23 | 1.0 | Initial implementation plan created | GitHub Copilot |

| 2025-08-23 | 1.1 | Started Story 3016: added CSV enum, connector skeleton, factory wiring, and DI registration | GitHub Copilot |

---

**Next Steps**:
1. Review and validate requirements with stakeholders
2. Refine effort estimates based on team capacity
3. Prioritize stories based on business value
4. Begin Phase 1 implementation
5. Set up continuous integration and testing pipeline

**Contact**: For questions or updates to this plan, please refer to Epic 2251 in Azure DevOps.
