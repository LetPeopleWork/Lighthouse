import CloseIcon from "@mui/icons-material/Close";
import {
	Box,
	Chip,
	Dialog,
	DialogContent,
	DialogTitle,
	IconButton,
	Link,
	Stack,
	Typography,
} from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import type {
	IFeatureDependency,
	NotHonouredReason,
} from "../../../models/FeatureDependency";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";

export interface DependencyDialogProps {
	featureName: string;
	dependencies: IFeatureDependency[];
	open: boolean;
	onClose: () => void;
}

const DependencyDialog: React.FC<DependencyDialogProps> = ({
	featureName,
	dependencies,
	open,
	onClose,
}) => {
	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const portfoliosTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIOS);

	// A withheld entry has no id of its own to be listed under - that is what withholding it means - so
	// it is counted instead. The list is read as it arrived and never reordered.
	const entries = useMemo(() => {
		let withheldSoFar = 0;

		return dependencies.map((dependency) => {
			if (!dependency.isWithheld) {
				return { key: dependency.referenceId, dependency };
			}

			withheldSoFar += 1;
			return { key: `withheld-${withheldSoFar}`, dependency };
		});
	}, [dependencies]);

	return (
		<Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
			<DialogTitle>
				{`${featuresTerm} ${featureName} depends on`}
				<IconButton
					aria-label="Close"
					onClick={onClose}
					sx={{ position: "absolute", right: 8, top: 8 }}
				>
					<CloseIcon />
				</IconButton>
			</DialogTitle>
			<DialogContent dividers>
				<Stack spacing={2}>
					{entries.map(({ key, dependency }) =>
						dependency.isWithheld ? (
							<Box key={key} data-testid="dependency-withheld">
								<Typography variant="body1">
									{`A ${featureTerm} you do not have access to`}
								</Typography>
								<Typography variant="body2" color="text.secondary">
									{reasonSentence(dependency.notHonouredReason, {
										featureTerm,
										portfoliosTerm,
										waitedOn: `a ${featureTerm} you do not have access to`,
									})}
								</Typography>
							</Box>
						) : (
							<Box key={key} data-testid="dependency-entry">
								<Link
									href={dependency.url ?? undefined}
									target="_blank"
									rel="noopener noreferrer"
									variant="body1"
								>
									{dependency.name}
								</Link>
								<Box
									sx={{
										display: "flex",
										gap: 1,
										alignItems: "center",
										mt: 0.5,
									}}
								>
									<Chip size="small" label={dependency.state} />
									{dependency.portfolios.map((portfolio) => (
										<Chip
											key={portfolio.id}
											size="small"
											variant="outlined"
											label={portfolio.name}
										/>
									))}
								</Box>
								{dependency.notHonouredReason ? (
									<Typography
										variant="body2"
										color="warning.main"
										data-testid={`dependency-reason-${dependency.referenceId}`}
									>
										{reasonSentence(dependency.notHonouredReason, {
											featureTerm,
											portfoliosTerm,
											waitedOn: dependency.name,
										})}
									</Typography>
								) : null}
							</Box>
						),
					)}
				</Stack>
			</DialogContent>
		</Dialog>
	);
};

// Built here from a code and a name, in this instance's own words - the same reason the warnings
// column composes its own sentences rather than printing one the server wrote.
const reasonSentence = (
	reason: NotHonouredReason | null,
	terms: { featureTerm: string; portfoliosTerm: string; waitedOn: string },
): string => {
	if (reason === "OutsideThisPortfolio") {
		return `This ${terms.featureTerm} and ${terms.waitedOn} share no ${terms.portfoliosTerm}. That dependency is not included in the forecast.`;
	}

	if (reason === "InALoop") {
		return `This ${terms.featureTerm} and ${terms.waitedOn} are waiting on each other. That dependency is not included in the forecast.`;
	}

	if (reason === "BlockerCannotBeForecast") {
		return `${terms.waitedOn} has no measured delivery to forecast from, so the wait cannot be given a date. That dependency is not included in the forecast.`;
	}

	return "";
};

export default DependencyDialog;
