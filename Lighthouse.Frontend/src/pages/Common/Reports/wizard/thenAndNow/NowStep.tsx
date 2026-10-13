import { Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import type { ReportTypeStepProps } from "../reportTypes";
import { nowLengthOf, type ThenAndNowDraft } from "./thenAndNowDraft";

/** Now always ends on the server's today; its length follows Then until changed. */
const NowStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	draft,
	onChange,
	disabled,
}) => {
	const nowLength = nowLengthOf(draft);

	return (
		<Stack spacing={2}>
			<TextField
				type="number"
				value={nowLength}
				disabled={disabled}
				onChange={(event) =>
					onChange({ ...draft, nowLength: event.target.value })
				}
				slotProps={{
					htmlInput: { "aria-label": "Now length in days", min: 1 },
				}}
				sx={{ width: 160 }}
			/>
			<Typography color="text.secondary">
				{`= the last ${nowLength} days, always ending today`}
			</Typography>
		</Stack>
	);
};

export default NowStep;
