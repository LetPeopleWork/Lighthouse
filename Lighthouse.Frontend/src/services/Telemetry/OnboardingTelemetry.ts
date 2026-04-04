type EntityType = "connection" | "team" | "portfolio";

type FailureCategory = "validation" | "network" | "license" | "unknown";

interface BaseProperties {
	entityType: EntityType;
	workTrackingSystem?: string;
	correlationId: string;
}

interface CreateEventProperties extends BaseProperties {
	wizardUsed: boolean | null;
}

interface FailureEventProperties extends CreateEventProperties {
	failureCategory: FailureCategory;
}

interface EditSaveEventProperties extends BaseProperties {
	failureCategory?: FailureCategory;
}

function emit(event: string, properties: object) {
	console.log(`[onboarding] ${event}`, properties);
}

export function generateCorrelationId(): string {
	return crypto.randomUUID();
}

export function emitCreateStarted(properties: CreateEventProperties) {
	emit("onboarding.create.started", properties);
}

export function emitCreateValidationStarted(properties: CreateEventProperties) {
	emit("onboarding.create.validation.started", properties);
}

export function emitCreateValidationSucceeded(
	properties: CreateEventProperties,
) {
	emit("onboarding.create.validation.succeeded", properties);
}

export function emitCreateValidationFailed(properties: FailureEventProperties) {
	emit("onboarding.create.validation.failed", properties);
}

export function emitCreateSucceeded(properties: CreateEventProperties) {
	emit("onboarding.create.succeeded", properties);
}

export function emitCreateFailed(properties: FailureEventProperties) {
	emit("onboarding.create.failed", properties);
}

export function emitWizardBranch(
	properties: CreateEventProperties & { branch: "wizard" | "manual" },
) {
	emit("onboarding.create.wizard-branch", properties);
}

export function emitEditSaveStarted(properties: EditSaveEventProperties) {
	emit("onboarding.edit.save.started", properties);
}

export function emitEditSaveSucceeded(properties: EditSaveEventProperties) {
	emit("onboarding.edit.save.succeeded", properties);
}

export function emitEditSaveFailed(
	properties: EditSaveEventProperties & { failureCategory: FailureCategory },
) {
	emit("onboarding.edit.save.failed", properties);
}
