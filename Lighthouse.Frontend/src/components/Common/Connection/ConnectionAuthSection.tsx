import { TextField, Typography } from "@mui/material";
import Grid from "@mui/material/Grid";
import type React from "react";
import type { IAuthenticationMethod } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";
import {
	resolveAuthOptionHelperText,
	secretCannotBeRead,
} from "./connectionFormRules";

interface ConnectionAuthSectionProps {
	authOptions: IWorkTrackingSystemOption[];
	selectedAuthMethod: IAuthenticationMethod | null;
	isEditMode: boolean;
	lockOAuthField: boolean;
	onOptionChange: (option: IWorkTrackingSystemOption, newValue: string) => void;
}

const ConnectionAuthSection: React.FC<ConnectionAuthSectionProps> = ({
	authOptions,
	selectedAuthMethod,
	isEditMode,
	lockOAuthField,
	onOptionChange,
}) => (
	<Grid size={{ xs: 12 }}>
		<Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>
			Authentication
		</Typography>
		<Grid container spacing={2}>
			{authOptions.map((option) => {
				const displayName =
					selectedAuthMethod?.options.find((o) => o.key === option.key)
						?.displayName ?? option.key;
				return (
					<Grid size={{ xs: 12, md: 6 }} key={option.key}>
						<TextField
							label={displayName}
							type={option.isSecret ? "password" : "text"}
							fullWidth
							value={option.value}
							placeholder={
								isEditMode && option.isSecret && option.value === ""
									? "Leave empty to keep existing value"
									: ""
							}
							slotProps={
								lockOAuthField ? { input: { readOnly: true } } : undefined
							}
							error={secretCannotBeRead(option)}
							helperText={resolveAuthOptionHelperText(option, lockOAuthField)}
							onChange={(e) => onOptionChange(option, e.target.value)}
						/>
					</Grid>
				);
			})}
		</Grid>
	</Grid>
);

export default ConnectionAuthSection;
