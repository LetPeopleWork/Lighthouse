import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import Typography from "@mui/material/Typography";
import { useNavigate } from "react-router";
import type { IRecentProblem } from "../../../../services/Api/LogService";
import LocalDateTimeDisplay from "../../../Common/LocalDateTimeDisplay/LocalDateTimeDisplay";

/**
 * Somebody who reads three entries as "three things have ever gone wrong" draws exactly the wrong
 * conclusion from a restart, so the section says what it is in words rather than leaving it to be
 * inferred from how short the list looks.
 */
const WHAT_THIS_SECTION_IS =
	"Only what has gone wrong since this instance started. Not a complete history, and not kept after a restart.";

/**
 * Where the rest of it still lives. Being shown the last few problems with no way on to the full log
 * is a dead end.
 */
const THE_FULL_LOG = "/settings?tab=system-info";

interface RecentProblemsSectionProps {
	/**
	 * What the instance said has gone wrong, in the order it gave them - newest first. `null` means it
	 * was asked and did not answer, which is not the same as having nothing to report: the section
	 * renders nothing at all rather than the reassuring empty state, because an unanswered question is
	 * not a clean bill of health.
	 */
	problems: IRecentProblem[] | null;
}

const RecentProblemsSection = ({ problems }: RecentProblemsSectionProps) => {
	const navigate = useNavigate();

	if (problems === null) {
		return null;
	}

	return (
		<>
			<Divider sx={{ my: 1.5 }} />

			<Typography variant="subtitle2" gutterBottom>
				Recent problems
			</Typography>

			{problems.length === 0 ? (
				<Typography variant="body2" color="text.secondary">
					Nothing has gone wrong since this instance started.
				</Typography>
			) : (
				problems.map((problem) => (
					<Typography
						key={`${problem.recordedAt}-${problem.level}-${problem.source}-${problem.message}`}
						data-testid="recent-problem-row"
						variant="body2"
						sx={{ py: 0.5 }}
					>
						<LocalDateTimeDisplay utcDate={problem.recordedAt} showTime /> —{" "}
						{problem.level} — {problem.message}
					</Typography>
				))
			)}

			<Typography
				variant="caption"
				color="text.secondary"
				sx={{ display: "block", mt: 1 }}
			>
				{WHAT_THIS_SECTION_IS}
			</Typography>

			<Button size="small" onClick={() => navigate(THE_FULL_LOG)}>
				Open the full log
			</Button>
		</>
	);
};

export default RecentProblemsSection;
