import { Stack, TextField, Typography } from "@mui/material";
import type React from "react";
import type { ReportTypeStepProps } from "../reportTypeSteps";
import type { ThenAndNowDraft } from "./thenAndNowDraft";

/** Now is as long as Then and always ends on the server's today. */
const NowStep: React.FC<Readonly<ReportTypeStepProps<ThenAndNowDraft>>> = ({
	draft,
}) => (
	<Stack spacing={2}>
		<TextField
			type="number"
			value={draft.thenLength}
			slotProps={{
				htmlInput: { "aria-label": "Now length in days", readOnly: true },
			}}
			sx={{ width: 160 }}
		/>
		<Typography color="text.secondary">
			{`= the last ${draft.thenLength} days, always ending today`}
		</Typography>
	</Stack>
);

export default NowStep;
