import TextField, { type TextFieldProps } from "@mui/material/TextField";
import type React from "react";

const QUERY_FIELD_MIN_ROWS = 4;
const QUERY_FIELD_MAX_ROWS = 20;

type QueryTextFieldProps = Omit<
	TextFieldProps,
	"multiline" | "rows" | "minRows" | "maxRows" | "slotProps"
> & {
	readonly readOnly?: boolean;
};

// A read-only field holds what a connector filled in, such as a whole CSV file, which nobody edits;
// letting it grow would push the rest of the form down for no gain.
const QueryTextField: React.FC<QueryTextFieldProps> = ({
	readOnly = false,
	...props
}) => {
	const rowProps = readOnly
		? { rows: QUERY_FIELD_MIN_ROWS }
		: { minRows: QUERY_FIELD_MIN_ROWS, maxRows: QUERY_FIELD_MAX_ROWS };

	return (
		<TextField
			{...props}
			{...rowProps}
			multiline
			slotProps={{ input: { readOnly } }}
		/>
	);
};

export default QueryTextField;
