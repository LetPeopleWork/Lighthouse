import React, { useEffect, useState } from "react";
import { ITeamSettings } from "../../../models/Team/TeamSettings";
import { Container, Typography, SelectChangeEvent } from "@mui/material";
import Grid from '@mui/material/Grid2'
import AdvancedInputsComponent from "../../../pages/Teams/Edit/AdvancedInputs";
import GeneralInputsComponent from "../../../pages/Teams/Edit/GeneralInputs";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WorkTrackingSystemComponent from "../WorkTrackingSystems/WorkTrackingSystemComponent";
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import TutorialButton from "../../App/LetPeopleWork/Tutorial/TutorialButton";
import TeamConfigurationTutorial from "../../App/LetPeopleWork/Tutorial/Tutorials/TeamConfigurationTutorial";
import StatesList from "../StatesList/StatesList";
import ValidationActions from "../ValidationActions/ValidationActions";

interface ModifyTeamSettingsProps {
    title: string;
    getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
    getTeamSettings: () => Promise<ITeamSettings>;
    saveTeamSettings: (settings: ITeamSettings) => Promise<void>;
    validateTeamSettings: (settings: ITeamSettings) => Promise<boolean>;
    modifyDefaultSettings?: boolean;
}

const ModifyTeamSettings: React.FC<ModifyTeamSettingsProps> = ({ title, getWorkTrackingSystems, getTeamSettings, saveTeamSettings, validateTeamSettings, modifyDefaultSettings = false }) => {
    const [loading, setLoading] = useState<boolean>(false);
    const [teamSettings, setTeamSettings] = useState<ITeamSettings | null>(null);
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [workTrackingSystems, setWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);
    const [inputsValid, setInputsValid] = useState<boolean>(false);

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

    const handleAddToDoState = (toDoState: string) => {
        if (toDoState.trim()) {
            setTeamSettings(prev => prev ? { ...prev, toDoStates: [...(prev.toDoStates || []), toDoState.trim()] } : prev);
        }
    };

    const handleRemoveToDoState = (toDoState: string) => {
        setTeamSettings(prev => prev ? { ...prev, toDoStates: (prev.toDoStates || []).filter(item => item !== toDoState) } : prev);
    };

    const handleAddDoingState = (doingState: string) => {
        if (doingState.trim()) {
            setTeamSettings(prev => prev ? { ...prev, doingStates: [...(prev.doingStates || []), doingState.trim()] } : prev);
        }
    };

    const handleRemoveDoingState = (doingState: string) => {
        setTeamSettings(prev => prev ? { ...prev, doingStates: (prev.doingStates || []).filter(item => item !== doingState) } : prev);
    };

    const handleAddDoneState = (doneState: string) => {
        if (doneState.trim()) {
            setTeamSettings(prev => prev ? { ...prev, doneStates: [...(prev.doneStates || []), doneState.trim()] } : prev);
        }
    };

    const handleRemoveDoneState = (doneState: string) => {
        setTeamSettings(prev => prev ? { ...prev, doneStates: (prev.doneStates || []).filter(item => item !== doneState) } : prev);
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
    }

    useEffect(() => {
        const handleStateChange = () => {
            const areInputsValid = teamSettings?.name != '' && (teamSettings?.throughputHistory ?? 0) > 0 && teamSettings?.featureWIP !== undefined &&
                (modifyDefaultSettings || teamSettings?.workItemQuery != '') && teamSettings?.workItemTypes.length > 0 && (modifyDefaultSettings || selectedWorkTrackingSystem !== null);

            setInputsValid(areInputsValid);
        };

        handleStateChange();
    }, [teamSettings, selectedWorkTrackingSystem, workTrackingSystems, modifyDefaultSettings]);

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

    const handleValidate = async () => {
        if (!teamSettings || modifyDefaultSettings) {
            return false;
        }

        const updatedSettings: ITeamSettings = { ...teamSettings, workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0 };
        return await validateTeamSettings(updatedSettings);
    };

    return (
        <LoadingAnimation isLoading={loading} hasError={false}>
            <Container maxWidth={false}>
                <Grid container justifyContent="flex-end" spacing={2}>
                    <Grid >
                        <TutorialButton
                            tutorialComponent={<TeamConfigurationTutorial />}
                        />
                    </Grid>
                </Grid>
                <Grid container spacing={3}>
                    <Grid size={{ xs: 12 }}>
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

                    <StatesList
                        toDoStates={teamSettings?.toDoStates || []}
                        onAddToDoState={handleAddToDoState}
                        onRemoveToDoState={handleRemoveToDoState}
                        doingStates={teamSettings?.doingStates || []}
                        onAddDoingState={handleAddDoingState}
                        onRemoveDoingState={handleRemoveDoingState}
                        doneStates={teamSettings?.doneStates || []}
                        onAddDoneState={handleAddDoneState}
                        onRemoveDoneState={handleRemoveDoneState}
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


                    <Grid size={{ xs: 12 }} sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
                        <ValidationActions 
                            onValidate={modifyDefaultSettings ? undefined : handleValidate}
                            onSave={handleSave}
                            inputsValid={inputsValid}
                            validationFailedMessage="Validation failed - either the connection failed, the query is invalid, or no closed items in the specified history could be found. Check the logs for additional details."
                        />
                    </Grid>
                </Grid>
            </Container>
        </LoadingAnimation>
    );
};

export default ModifyTeamSettings;
