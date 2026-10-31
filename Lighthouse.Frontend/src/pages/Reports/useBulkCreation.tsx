import AddIcon from "@mui/icons-material/Add";
import { Button, CircularProgress } from "@mui/material";
import { useRef, useState } from "react";
import { LicenseTooltip } from "../../components/App/License/LicenseToolTip";
import { useLicenseRestrictions } from "../../hooks/useLicenseRestrictions";
import type { RbacState } from "../../hooks/useRbac";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import BulkReportWizard from "./BulkReportWizard";
import type { OwnersStatus } from "./OwnerStep";
import { useReportCreationOptions } from "./useReportCreationOptions";

/** Whether the viewer administers at least one Team or Portfolio, so has somewhere to create reports. */
const isAdminOfAny = ({
	isRbacEnabled,
	isSystemAdmin,
	summary,
}: RbacState): boolean =>
	!isRbacEnabled ||
	isSystemAdmin ||
	(summary.adminTeamIds?.length ?? 0) > 0 ||
	(summary.adminPortfolioIds?.length ?? 0) > 0;

/** A failed permission load is never read as "Admin everywhere" by the owner step, whatever the hook answers elsewhere. */
const ownersStatusOf = ({ isLoading, loadFailed }: RbacState): OwnersStatus => {
	if (isLoading) return "loading";
	return loadFailed ? "failed" : "ready";
};

/** Create reports, and the wizard it opens once what a report needs has arrived for this opening. */
export const useBulkCreation = (rbac: RbacState) => {
	// Counts every opening and never goes back, so no opening shares its key, or an earlier answer, with another.
	const openings = useRef(0);
	const [opening, setOpening] = useState<number | null>(null);
	const [retrying, setRetrying] = useState(false);
	const options = useReportCreationOptions(opening);
	const { licenseStatus, status } = useLicenseRestrictions();
	const { getTerm } = useTerminology();
	const isPremium = licenseStatus?.canUsePremiumFeatures === true;
	const lacksAdmin = !rbac.isLoading && !isAdminOfAny(rbac);
	const open = () => {
		setRetrying(false);
		openings.current += 1;
		setOpening(openings.current);
	};
	const close = () => {
		setRetrying(false);
		setOpening(null);
	};
	const retry = () => {
		setRetrying(true);
		options.refetch();
	};
	// The first answer opens the wizard, and from then on it is the reader's until they close it: a later read, or one
	// that fails, never takes their draft away. Once it has said it could not load, it stays open while Retry asks again.
	const arrived = options.data !== undefined;
	const showWizard =
		opening !== null && (arrived || options.isError || retrying);
	const waitingBeforeWizard = options.isFetching && !showWizard;
	const button = (
		<LicenseTooltip
			canUseFeature={isPremium}
			defaultTooltip={
				lacksAdmin
					? `You are not Admin of any ${getTerm(TERMINOLOGY_KEYS.TEAM)} or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}.`
					: ""
			}
			checkStatus={licenseStatus ? "ready" : status}
		>
			<span>
				<Button
					variant="contained"
					startIcon={
						waitingBeforeWizard ? <CircularProgress size={20} /> : <AddIcon />
					}
					disabled={
						!isPremium || rbac.isLoading || lacksAdmin || options.isFetching
					}
					onClick={open}
				>
					Create reports
				</Button>
			</span>
		</LicenseTooltip>
	);
	const wizard = showWizard ? (
		<BulkReportWizard
			key={opening}
			options={arrived ? options.data : undefined}
			isTeamAdmin={rbac.isTeamAdmin}
			isPortfolioAdmin={rbac.isPortfolioAdmin}
			ownersStatus={ownersStatusOf(rbac)}
			onRetryOwners={rbac.retry}
			retrying={options.isFetching}
			onRetry={retry}
			onClose={close}
		/>
	) : null;
	return { button, wizard };
};
