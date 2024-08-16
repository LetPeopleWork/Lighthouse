import React, { useEffect, useState } from "react";
import { ITeamSettings } from "../../../models/Team/TeamSettings";
import { Container, Grid, Typography, SelectChangeEvent } from "@mui/material";
import AdvancedInputsComponent from "../../../pages/Teams/Edit/AdvancedInputs";
import GeneralInputsComponent from "../../../pages/Teams/Edit/GeneralInputs";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WorkTrackingSystemComponent from "../WorkTrackingSystems/WorkTrackingSystemComponent";
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import ActionButton from "../ActionButton/ActionButton";
import TutorialButton from "../../App/LetPeopleWork/Tutorial/TutorialButton";
import TeamConfigurationTutorial from "../../App/LetPeopleWork/Tutorial/Tutorials/TeamConfigurationTutorial";

interface ModifyTeamSettingsProps {
    title: string;
    getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
    getTeamSettings: () => Promise<ITeamSettings>;
    saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
    modifyDefaultSettings? : boolean;
}

const ModifyTeamSettings: React.FC<ModifyTeamSettingsProps> = ({ title, getWorkTrackingSystems, getTeamSettings, saveTeamSettings, modifyDefaultSettings = false }) => {
    const [loading, setLoading] = useState<boolean>(false);
    const [teamSettings, setTeamSettings] = useState<ITeamSettings | null>(null);
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [workTrackingSystems, setWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);
    const [formValid, setFormValid] = useState<boolean>(false);

    const handleTeamSettingsChange = (key: keyof ITeamSettings, value: string | number) => {
        setTeamSettings(prev => prev ? { ...prev, [key]: value } : prev);
    };

    const handleAddWorkItemType = (newWorkItemType: string) => {
        if (newWorkItemType.trim()) {
            setTeamSettings(prev => prev ? { ...prev, workItemTypes: [...(prev.workItemTypes || []), newWorkItemType.trim()] } : prev);
        }
    };

    const handleRemoveWorkItemType = (type: string) => {
        setTeamSettings(prev => prev ? { ...prev, workItemTypes: (prev.workItemTypes || []).filter(item => item !== type) } : prev);
    };

    const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
        const selectedWorkTrackingSystemName = event.target.value;
        const selectedWorkTrackingSystem = workTrackingSystems.find(system => system.name === selectedWorkTrackingSystemName) ?? null;

        setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);
    };

    const handleOnNewWorkTrackingSystemConnectionAddedDialogClosed = async (newConnection: IWorkTrackingSystemConnection) => {
        setWorkTrackingSystems(prevSystems => [...prevSystems, newConnection]);
        setSelectedWorkTrackingSystem(newConnection);
    };

    const handleSave = async () => {
        if (!teamSettings) {
            return;
        }

        const updatedSettings: ITeamSettings = { ...teamSettings, workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0 };
        await saveTeamSettings(updatedSettings);

        setFormValid(false);
    }

    useEffect(() => {
        const handleStateChange = () => {
            const isFormValid = teamSettings?.name != '' && (teamSettings?.throughputHistory ?? 0) > 0 && teamSettings?.featureWIP !== undefined &&
            (modifyDefaultSettings || teamSettings?.workItemQuery != '') && teamSettings?.workItemTypes.length > 0 && (modifyDefaultSettings || selectedWorkTrackingSystem !== null);

            setFormValid(isFormValid);
        };

        handleStateChange();
    }, [teamSettings, selectedWorkTrackingSystem, workTrackingSystems]);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const settings = await getTeamSettings();
                setTeamSettings(settings);

                const workTrackingSystemConnections = await getWorkTrackingSystems();
                setWorkTrackingSystems(workTrackingSystemConnections);

                setSelectedWorkTrackingSystem(workTrackingSystemConnections.find(system => system.id === settings.workTrackingSystemConnectionId) ?? null);

            } catch (error) {
                console.error('Error fetching data', error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [getTeamSettings, getWorkTrackingSystems]);

    return (
        <LoadingAnimation isLoading={loading} hasError={false}>
            <Container>
                <Grid container justifyContent="flex-end" spacing={2}>
                    <Grid item>
                        <TutorialButton
                            tutorialComponent={<TeamConfigurationTutorial />}
                        />
                    </Grid>
                </Grid>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h4'>{title}</Typography>
                    </Grid>

                    <GeneralInputsComponent
                        teamSettings={teamSettings}
                        onTeamSettingsChange={handleTeamSettingsChange}
                    />

                    <WorkItemTypesComponent
                        workItemTypes={teamSettings?.workItemTypes || []}
                        onAddWorkItemType={handleAddWorkItemType}
                        onRemoveWorkItemType={handleRemoveWorkItemType}
                    />

                    {!modifyDefaultSettings ? (
                        <WorkTrackingSystemComponent
                            workTrackingSystems={workTrackingSystems}
                            selectedWorkTrackingSystem={selectedWorkTrackingSystem}
                            onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
                            onNewWorkTrackingSystemConnectionAdded={handleOnNewWorkTrackingSystemConnectionAddedDialogClosed}
                        />) : (
                        <></>
                    )}

                    <AdvancedInputsComponent
                        teamSettings={teamSettings}
                        onTeamSettingsChange={handleTeamSettingsChange}
                    />

                    <Grid item xs={12}>
                        <ActionButton
                            buttonVariant="contained"
                            buttonText="Save"
                            onClickHandler={handleSave}
                            disabled={!formValid} />
                    </Grid>
                </Grid>
            </Container>
        </LoadingAnimation>
    );
};

export default ModifyTeamSettings;
