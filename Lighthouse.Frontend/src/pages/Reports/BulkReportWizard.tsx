import {
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Typography,
} from "@mui/material";
import type React from "react";
import { useMemo, useState } from "react";
import WizardStepperFrame, {
	type WizardFrameAction,
} from "../../components/Common/CreateWizards/WizardStepperFrame";
import { useRbac } from "../../hooks/useRbac";
import type { IReportTemplates } from "../../models/Reports/Report";
import type {
	IReportCreationOptions,
	IReportCreationOwner,
} from "../../models/Reports/ReportsAcrossOwners";
import {
	type ReportTypeStep,
	reportTypes,
	templatesForOwnersOf,
} from "../Common/Reports/wizard/reportTypes";
import TypeStep from "../Common/Reports/wizard/TypeStep";
import BulkResult, { type BulkRun } from "./BulkResult";
import OwnerStep, { ownerKeyOf } from "./OwnerStep";
import { useBulkReportCreation } from "./useBulkReportCreation";

const TITLE_ID = "bulk-report-wizard-title";
const TYPE_STEP = 0;
const chosen = "then-and-now";
const type = reportTypes[chosen];
type Draft = ReturnType<typeof type.forOwners.newDraft>;

/** Type, then the type's own steps, then who gets the report. */
const STEP_LABELS = ["Type", ...type.steps.map((step) => step.label), "Owners"];
const OWNER_STEP = STEP_LABELS.length - 1;

const createLabelOf = (count: number): string =>
	count === 1 ? "Create 1 report" : `Create ${count} reports`;

const byName = (left: IReportCreationOwner, right: IReportCreationOwner) =>
	left.ownerName.localeCompare(right.ownerName);

/** The Teams the reader may create on, as the owner step lists them. */
const useTeamsToCreateOn = (
	options: IReportCreationOptions,
): IReportCreationOwner[] => {
	const { isTeamAdmin } = useRbac();
	return useMemo(
		() =>
			options.owners
				.filter(
					(owner) => owner.ownerKind === "team" && isTeamAdmin(owner.ownerId),
				)
				.sort(byName),
		[options.owners, isTeamAdmin],
	);
};

const typeStepAt = (activeStep: number): ReportTypeStep<Draft> | undefined =>
	activeStep > TYPE_STEP && activeStep < OWNER_STEP
		? type.steps[activeStep - 1]
		: undefined;

const nextActionOf = (
	activeStep: number,
	draft: Draft,
	templates: IReportTemplates,
	onNext: () => void,
): WizardFrameAction | undefined => {
	if (activeStep === OWNER_STEP) return undefined;
	const step = typeStepAt(activeStep);
	return {
		label: "Next",
		onClick: onNext,
		disabled: step !== undefined && !step.isComplete(draft, templates),
	};
};

interface StepContentProps {
	activeStep: number;
	templates: IReportTemplates;
	draft: Draft;
	onChange: (draft: Draft) => void;
	teams: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	onToggle: (owner: IReportCreationOwner) => void;
}

/** No owner is known while the report is configured; the window steps ask for none of an owner's own. */
const NO_OWNER_YET = { kind: "team", id: 0 } as const;

const StepContent: React.FC<Readonly<StepContentProps>> = ({
	activeStep,
	templates,
	draft,
	onChange,
	teams,
	picked,
	onToggle,
}) => {
	if (activeStep === OWNER_STEP) {
		return (
			<OwnerStep
				teams={teams}
				picked={picked}
				onToggle={onToggle}
				stepNumber={activeStep + 1}
				stepCount={STEP_LABELS.length}
				disabled={false}
			/>
		);
	}
	const step = typeStepAt(activeStep);
	if (step === undefined) {
		return (
			<TypeStep status="ready" chosen={chosen} onRetry={() => undefined} />
		);
	}
	const Component = step.component;
	return (
		<Component
			owner={NO_OWNER_YET}
			templates={templates}
			draft={draft}
			onChange={onChange}
			disabled={false}
			windowPresets={type.forOwners.windowPresets}
		/>
	);
};

const runOf = (
	creation: ReturnType<typeof useBulkReportCreation>,
): BulkRun | null => {
	if (creation.isPending) return { status: "pending" };
	if (creation.isError) return { status: "error" };
	if (creation.isSuccess) return { status: "success", outcomes: creation.data };
	return null;
};

interface ReadyWizardProps {
	options: IReportCreationOptions;
	onClose: () => void;
}

/** One report, configured once, created for every Team picked; the single wizard's steps, then the owners. */
const ReadyWizard: React.FC<Readonly<ReadyWizardProps>> = ({
	options,
	onClose,
}) => {
	const templates = useMemo(() => templatesForOwnersOf(options), [options]);
	const teams = useTeamsToCreateOn(options);
	const creation = useBulkReportCreation();
	const [activeStep, setActiveStep] = useState(TYPE_STEP);
	const [draft, setDraft] = useState(type.forOwners.newDraft);
	const [picked, setPicked] = useState<ReadonlySet<string>>(new Set());

	const toggle = (owner: IReportCreationOwner) => {
		const next = new Set(picked);
		const key = ownerKeyOf(owner);
		if (!next.delete(key)) next.add(key);
		setPicked(next);
	};

	const pickedTeams = teams.filter((team) => picked.has(ownerKeyOf(team)));

	const create = () =>
		creation.mutate({
			...type.forOwners.toNewReport(draft, templates),
			owners: pickedTeams.map(({ ownerKind, ownerId }) => ({
				ownerKind,
				ownerId,
			})),
		});

	const run = runOf(creation);

	return (
		<Dialog
			open
			onClose={run?.status === "pending" ? undefined : onClose}
			aria-labelledby={TITLE_ID}
			maxWidth="md"
			fullWidth
		>
			<DialogTitle id={TITLE_ID}>Create reports</DialogTitle>
			{run === null ? (
				<WizardStepperFrame
					steps={STEP_LABELS}
					activeStep={activeStep}
					busy={false}
					onCancel={onClose}
					onBack={
						activeStep > TYPE_STEP
							? () => setActiveStep(activeStep - 1)
							: undefined
					}
					next={nextActionOf(activeStep, draft, templates, () =>
						setActiveStep(activeStep + 1),
					)}
					primaryAction={
						activeStep === OWNER_STEP
							? {
									label: createLabelOf(pickedTeams.length),
									onClick: create,
									disabled: pickedTeams.length === 0,
								}
							: undefined
					}
				>
					<StepContent
						activeStep={activeStep}
						templates={templates}
						draft={draft}
						onChange={setDraft}
						teams={teams}
						picked={picked}
						onToggle={toggle}
					/>
				</WizardStepperFrame>
			) : (
				<BulkResult count={pickedTeams.length} run={run} onDone={onClose} />
			)}
		</Dialog>
	);
};

const OPTIONS_COULD_NOT_LOAD = "What a report needs couldn't be loaded.";

interface CouldNotLoadProps {
	retrying: boolean;
	onRetry: () => void;
	onClose: () => void;
}

/** No step is shown without what a report needs: the reader may only leave or ask again. */
const CouldNotLoad: React.FC<Readonly<CouldNotLoadProps>> = ({
	retrying,
	onRetry,
	onClose,
}) => (
	<Dialog open onClose={onClose} aria-labelledby={TITLE_ID} maxWidth="sm">
		<DialogTitle id={TITLE_ID}>What a report needs</DialogTitle>
		<DialogContent>
			<Typography color={retrying ? "text.disabled" : undefined}>
				{OPTIONS_COULD_NOT_LOAD}
			</Typography>
		</DialogContent>
		<DialogActions>
			<Button onClick={onClose}>Cancel</Button>
			<Button
				variant="contained"
				onClick={onRetry}
				disabled={retrying}
				startIcon={retrying ? <CircularProgress size={20} /> : undefined}
			>
				Retry
			</Button>
		</DialogActions>
	</Dialog>
);

interface BulkReportWizardProps {
	/** Undefined when it could not be loaded, or while Retry asks again. */
	options: IReportCreationOptions | undefined;
	retrying: boolean;
	onRetry: () => void;
	onClose: () => void;
}

const BulkReportWizard: React.FC<Readonly<BulkReportWizardProps>> = ({
	options,
	retrying,
	onRetry,
	onClose,
}) =>
	options === undefined ? (
		<CouldNotLoad retrying={retrying} onRetry={onRetry} onClose={onClose} />
	) : (
		<ReadyWizard options={options} onClose={onClose} />
	);

export default BulkReportWizard;
