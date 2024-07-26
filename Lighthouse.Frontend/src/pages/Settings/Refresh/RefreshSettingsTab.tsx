import React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import RefreshSettingUpdater from "./RefreshSettingUpdater";

const RefreshSettingsTab: React.FC = () => {
    return (
        <>
            <InputGroup title="Throughput Refresh">
                <RefreshSettingUpdater settingName="Throughput" />
            </InputGroup>
            <InputGroup title="Feature Refresh">
                <RefreshSettingUpdater settingName="Feature" />
            </InputGroup>
            <InputGroup title="Forecast Refresh" >
                <RefreshSettingUpdater settingName="Forecast" />
            </InputGroup>
        </>
    )
}

export default RefreshSettingsTab;