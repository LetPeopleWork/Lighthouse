import type React from "react";
import { useContext, useEffect, useState } from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import { useRbac } from "../../../hooks/useRbac";
import type {
	IWorkItemRuleCondition,
	IWorkItemRuleSchema,
} from "../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { DeliveryRuleBuilder } from "../../Common/DeliveryRuleBuilder/DeliveryRuleBuilder";

interface ForecastFilterEditorProps {
	teamId: number;
	rules?: IWorkItemRuleCondition[];
	onChange?: (rules: IWorkItemRuleCondition[]) => void;
}

const EXCLUSION_TITLE = "Exclude items where…";
const EXCLUSION_EMPTY_STATE =
	"Add at least one rule to exclude work items from forecast throughput.";

const ForecastFilterEditor: React.FC<ForecastFilterEditorProps> = ({
	teamId,
	rules = [],
	onChange,
}) => {
	const { teamService } = useContext(ApiServiceContext);
	const { isTeamAdmin } = useRbac();
	const { licenseStatus } = useLicenseRestrictions();
	const isPremium = licenseStatus?.canUsePremiumFeatures ?? true;
	const [schema, setSchema] = useState<IWorkItemRuleSchema | null>(null);

	useEffect(() => {
		if (!isPremium) {
			return;
		}

		let cancelled = false;
		teamService
			.getForecastFilterSchema(teamId)
			.then((data) => {
				if (!cancelled) {
					setSchema(data);
				}
			})
			.catch(() => {
				if (!cancelled) {
					setSchema(null);
				}
			});

		return () => {
			cancelled = true;
		};
	}, [teamService, teamId, isPremium]);

	if (!isPremium) {
		return null;
	}

	if (schema === null) {
		return null;
	}

	const readOnly = !isTeamAdmin(teamId);
	const handleChange = onChange ?? (() => undefined);

	return (
		<DeliveryRuleBuilder
			rules={rules}
			onChange={handleChange}
			fields={schema.fields}
			operators={schema.operators}
			maxRules={schema.maxRules}
			maxValueLength={schema.maxValueLength}
			disabled={readOnly}
			title={EXCLUSION_TITLE}
			emptyStateMessage={EXCLUSION_EMPTY_STATE}
		/>
	);
};

export default ForecastFilterEditor;
