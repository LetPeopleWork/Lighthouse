import React from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";

const GeneralSettings: React.FC = () => {
    return (
        <>
            <InputGroup title="Refresh Settings">
                <></>
            </InputGroup><InputGroup title="Team Default Settings" initiallyExpanded={false}>
                <></>
            </InputGroup><InputGroup title="Project Default Settings" initiallyExpanded={false}>
                <></>
            </InputGroup>
        </>
    )
}

export default GeneralSettings;