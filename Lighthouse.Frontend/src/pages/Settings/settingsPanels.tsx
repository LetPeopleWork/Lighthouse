import ArticleIcon from "@mui/icons-material/Article";
import DatasetIcon from "@mui/icons-material/Dataset";
import EventBusyIcon from "@mui/icons-material/EventBusy";
import InsightsIcon from "@mui/icons-material/Insights";
import KeyIcon from "@mui/icons-material/Key";
import LockIcon from "@mui/icons-material/Lock";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import StorageIcon from "@mui/icons-material/Storage";
import SyncIcon from "@mui/icons-material/Sync";
import TranslateIcon from "@mui/icons-material/Translate";
import type React from "react";
import type { SettingsTabSlug } from "../../models/Settings/settingsAreas";
import ApiKeysSettings from "./ApiKeys/ApiKeysSettings";
import DatabaseManagementSettings from "./DatabaseManagement/DatabaseManagementSettings";
import DemoDataSettings from "./DemoData/DemoDataSettings";
import EncryptionPanel from "./Encryption/EncryptionPanel";
import CalendarTab from "./Organization/CalendarTab";
import RefreshAndOrderingTab from "./Organization/RefreshAndOrderingTab";
import TerminologyTab from "./Organization/TerminologyTab";
import RbacSettings from "./Rbac/RbacSettings";
import UsageDataTab from "./System/UsageDataTab";
import SystemInfoSettings from "./SystemInfo/SystemInfoSettings";

interface TabPresentation {
	readonly icon: React.ReactElement;
	readonly panel: React.ReactElement;
}

/** What each tab of the definition looks like and which panel it opens. */
export const settingsPanels: Readonly<
	Record<SettingsTabSlug, TabPresentation>
> = {
	terminology: { icon: <TranslateIcon />, panel: <TerminologyTab /> },
	calendar: { icon: <EventBusyIcon />, panel: <CalendarTab /> },
	"refresh-ordering": {
		icon: <SyncIcon />,
		panel: <RefreshAndOrderingTab />,
	},
	"system-wide": { icon: <ManageAccountsIcon />, panel: <RbacSettings /> },
	"api-keys": { icon: <KeyIcon />, panel: <ApiKeysSettings /> },
	"demo-data": { icon: <DatasetIcon />, panel: <DemoDataSettings /> },
	database: {
		icon: <StorageIcon />,
		panel: <DatabaseManagementSettings />,
	},
	encryption: { icon: <LockIcon />, panel: <EncryptionPanel /> },
	"usage-data": { icon: <InsightsIcon />, panel: <UsageDataTab /> },
	"system-info": { icon: <ArticleIcon />, panel: <SystemInfoSettings /> },
};
