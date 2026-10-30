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
import OwnerStep, {
	isOverTheCeiling,
	OVER_THE_CEILING_ID,
	type OwnersStatus,
	ownerKeyOf,
} from "./OwnerStep";
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

type IsAdmin = (ownerId: number) => boolean;

/** The owners of one kind the reader may create on, as the owner step lists them. */
const useOwnersToCreateOn = (
	options: IReportCreationOptions,
	ownerKind: IReportCreationOwner["ownerKind"],
	isAdmin: IsAdmin,
): IReportCreationOwner[] =>
	useMemo(
		() =>
			options.owners
				.filter(
					(owner) => owner.ownerKind === ownerKind && isAdmin(owner.ownerId),
				)
				.sort(byName),
		[options.owners, ownerKind, isAdmin],
	);

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

const createActionOf = (
	activeStep: number,
	pickedCount: number,
	ownersStatus: OwnersStatus,
	onCreate: () => void,
): WizardFrameAction | undefined =>
	activeStep === OWNER_STEP
		? {
				label: createLabelOf(pickedCount),
				onClick: onCreate,
				disabled:
					ownersStatus !== "ready" ||
					pickedCount === 0 ||
					isOverTheCeiling(pickedCount),
				describedBy: isOverTheCeiling(pickedCount)
					? OVER_THE_CEILING_ID
					: undefined,
			}
		: undefined;

interface StepContentProps {
	activeStep: number;
	templates: IReportTemplates;
	draft: Draft;
	onChange: (draft: Draft) => void;
	ownersStatus: OwnersStatus;
	onRetryOwners: () => void;
	teams: readonly IReportCreationOwner[];
	portfolios: readonly IReportCreationOwner[];
	picked: ReadonlySet<string>;
	onToggle: (owner: IReportCreationOwner) => void;
	onSelectAll: (owners: readonly IReportCreationOwner[]) => void;
}

/** No owner is known while the report is configured; the window steps ask for none of an owner's own. */
const NO_OWNER_YET = { kind: "team", id: 0 } as const;

const StepContent: React.FC<Readonly<StepContentProps>> = ({
	activeStep,
	templates,
	draft,
	onChange,
	ownersStatus,
	onRetryOwners,
	teams,
	portfolios,
	picked,
	onToggle,
	onSelectAll,
}) => {
	if (activeStep === OWNER_STEP) {
		return (
			<OwnerStep
				ownersStatus={ownersStatus}
				onRetry={onRetryOwners}
				teams={teams}
				portfolios={portfolios}
				picked={picked}
				onToggle={onToggle}
				onSelectAll={onSelectAll}
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
			forSeveralOwners
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
	isTeamAdmin: IsAdmin;
	isPortfolioAdmin: IsAdmin;
	ownersStatus: OwnersStatus;
	onRetryOwners: () => void;
	onClose: () => void;
}

/** One report, configured once, created for every Team and Portfolio picked; the single wizard's steps, then the owners. */
const ReadyWizard: React.FC<Readonly<ReadyWizardProps>> = ({
	options,
	isTeamAdmin,
	isPortfolioAdmin,
	ownersStatus,
	onRetryOwners,
	onClose,
}) => {
	const templates = useMemo(() => templatesForOwnersOf(options), [options]);
	const teams = useOwnersToCreateOn(options, "team", isTeamAdmin);
	const portfolios = useOwnersToCreateOn(
		options,
		"portfolio",
		isPortfolioAdmin,
	);
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

	const selectAll = (owners: readonly IReportCreationOwner[]) =>
		setPicked(
			new Set([...picked, ...owners.map((owner) => ownerKeyOf(owner))]),
		);

	const pickedOwners = [...teams, ...portfolios].filter((owner) =>
		picked.has(ownerKeyOf(owner)),
	);

	const create = () =>
		creation.mutate({
			...type.forOwners.toNewReport(draft, templates),
			owners: pickedOwners.map(({ ownerKind, ownerId }) => ({
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
					primaryAction={createActionOf(
						activeStep,
						pickedOwners.length,
						ownersStatus,
						create,
					)}
				>
					<StepContent
						activeStep={activeStep}
						templates={templates}
						draft={draft}
						onChange={setDraft}
						ownersStatus={ownersStatus}
						onRetryOwners={onRetryOwners}
						teams={teams}
						portfolios={portfolios}
						picked={picked}
						onToggle={toggle}
						onSelectAll={selectAll}
					/>
				</WizardStepperFrame>
			) : (
				<BulkResult
					count={pickedOwners.length}
					minimumLengthDays={options.minimumLengthDays}
					run={run}
					onDone={onClose}
				/>
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
	/** The page's own read of the viewer's roles, so the wizard lists only Teams and Portfolios the viewer is Admin of from its first frame. */
	isTeamAdmin: IsAdmin;
	isPortfolioAdmin: IsAdmin;
	/** Whether the viewer's roles are on their way, could not be loaded, or are known, so the owner step never lists on a guess. */
	ownersStatus: OwnersStatus;
	onRetryOwners: () => void;
	retrying: boolean;
	onRetry: () => void;
	onClose: () => void;
}

const BulkReportWizard: React.FC<Readonly<BulkReportWizardProps>> = ({
	options,
	isTeamAdmin,
	isPortfolioAdmin,
	ownersStatus,
	onRetryOwners,
	retrying,
	onRetry,
	onClose,
}) =>
	options === undefined ? (
		<CouldNotLoad retrying={retrying} onRetry={onRetry} onClose={onClose} />
	) : (
		<ReadyWizard
			options={options}
			isTeamAdmin={isTeamAdmin}
			isPortfolioAdmin={isPortfolioAdmin}
			ownersStatus={ownersStatus}
			onRetryOwners={onRetryOwners}
			onClose={onClose}
		/>
	);

export default BulkReportWizard;
