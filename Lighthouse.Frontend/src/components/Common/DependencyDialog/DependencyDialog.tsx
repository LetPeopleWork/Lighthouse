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
import type { IFeatureDependency } from "../../../models/FeatureDependency";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	reasonSentence,
	withheldName,
} from "../../../utils/dependencies/dependencySentences";

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
	const terms = {
		featureTerm,
		portfolioTerm: getTerm(TERMINOLOGY_KEYS.PORTFOLIO),
	};

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
								<Typography variant="body1">{withheldName(terms)}</Typography>
								<Typography variant="body2" color="text.secondary">
									{reasonSentence(
										dependency.notHonouredReason,
										withheldName(terms),
										terms,
									)}
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
										{reasonSentence(
											dependency.notHonouredReason,
											dependency.name,
											terms,
										)}
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

export default DependencyDialog;
