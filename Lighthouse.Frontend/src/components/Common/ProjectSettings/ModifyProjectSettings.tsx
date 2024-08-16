import { Container, Grid, Typography, SelectChangeEvent } from "@mui/material";
import React, { useEffect, useState } from "react";
import LoadingAnimation from "../LoadingAnimation/LoadingAnimation";
import MilestonesComponent from "../Milestones/MilestonesComponent";
import WorkItemTypesComponent from "../WorkItemTypes/WorkItemTypesComponent";
import WorkTrackingSystemComponent from "../WorkTrackingSystems/WorkTrackingSystemComponent";
import { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { IProjectSettings } from "../../../models/Project/ProjectSettings";
import GeneralInputsComponent from "../../../pages/Projects/Edit/GeneralInputs";
import { IMilestone } from "../../../models/Project/Milestone";
import AdvancedInputsComponent from "../../../pages/Projects/Edit/AdvancedInputs";
import ActionButton from "../ActionButton/ActionButton";
import ProjectConfigurationTutorial from "../../App/LetPeopleWork/Tutorial/Tutorials/ProjectConfigurationTutorial";
import TutorialButton from "../../App/LetPeopleWork/Tutorial/TutorialButton";

interface ModifyProjectSettingsProps {
    title: string;
    getWorkTrackingSystems: () => Promise<IWorkTrackingSystemConnection[]>;
    getProjectSettings: () => Promise<IProjectSettings>;
    saveProjectSettings: (settings: IProjectSettings) => Promise<void>;
    modifyDefaultSettings? : boolean;
}

const ModifyProjectSettings: React.FC<ModifyProjectSettingsProps> = ({ title, getWorkTrackingSystems, getProjectSettings, saveProjectSettings, modifyDefaultSettings = false }) => {
    const [loading, setLoading] = useState<boolean>(false);
    const [projectSettings, setProjectSettings] = useState<IProjectSettings | null>(null);
    const [workTrackingSystems, setWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [formValid, setFormValid] = useState<boolean>(false);

    const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
        const selectedWorkTrackingSystemName = event.target.value;
        const selectedWorkTrackingSystem = workTrackingSystems.find(system => system.name === selectedWorkTrackingSystemName) ?? null;

        setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);
    }; const handleOnNewWorkTrackingSystemConnectionAddedDialogClosed = async (newConnection: IWorkTrackingSystemConnection) => {
        setWorkTrackingSystems(prevSystems => [...prevSystems, newConnection]);
        setSelectedWorkTrackingSystem(newConnection);
    };

    const handleAddWorkItemType = (newWorkItemType: string) => {
        if (newWorkItemType.trim()) {
            setProjectSettings(prev => prev ? { ...prev, workItemTypes: [...(prev.workItemTypes || []), newWorkItemType.trim()] } : prev);
        }
    };

    const handleRemoveWorkItemType = (type: string) => {
        setProjectSettings(prev => prev ? { ...prev, workItemTypes: (prev.workItemTypes || []).filter(item => item !== type) } : prev);
    };

    const handleProjectSettingsChange = (key: keyof IProjectSettings, value: string | number) => {
        setProjectSettings(prev => prev ? { ...prev, [key]: value } : prev);
    };

    const handleAddMilestone = (milestone: IMilestone) => {
        setProjectSettings(prev => prev ? { ...prev, milestones: [...(prev.milestones || []), milestone] } : prev);
    };

    const handleRemoveMilestone = (name: string) => {
        setProjectSettings(prev => prev ? { ...prev, milestones: (prev.milestones || []).filter(milestone => milestone.name !== name) } : prev);
    };

    const handleUpdateMilestone = (name: string, updatedMilestone: Partial<IMilestone>) => {
        setProjectSettings(prev => prev ? {
            ...prev,
            milestones: (prev.milestones || []).map(milestone =>
                milestone.name === name ? { ...milestone, ...updatedMilestone } : milestone
            )
        } : prev);
    };

    const handleSave = async () => {
        if (!projectSettings) {
            return;
        }

        const updatedSettings: IProjectSettings = { ...projectSettings, workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0 };
        await saveProjectSettings(updatedSettings);

        setFormValid(false);
    }

    useEffect(() => {
        const handleStateChange = () => {
            const isFormValid = projectSettings?.name != '' && projectSettings?.defaultAmountOfWorkItemsPerFeature !== undefined &&
            (modifyDefaultSettings || projectSettings?.workItemQuery != '') && projectSettings?.workItemTypes.length > 0 && (modifyDefaultSettings  || selectedWorkTrackingSystem !== null);

            setFormValid(isFormValid);
        };

        handleStateChange();
    }, [projectSettings, selectedWorkTrackingSystem, workTrackingSystems]);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const settings = await getProjectSettings();
                setProjectSettings(settings);

                const systems = await getWorkTrackingSystems();
                setWorkTrackingSystems(systems);

                setSelectedWorkTrackingSystem(systems.find(system => system.id === settings.workTrackingSystemConnectionId) ?? null);

            } catch (error) {
                console.error('Error fetching data', error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [getProjectSettings, getWorkTrackingSystems]);

    return (
        <LoadingAnimation isLoading={loading} hasError={false} >
            <Container>
                <Grid container justifyContent="flex-end" spacing={2}>
                    <Grid item>
                        <TutorialButton
                            tutorialComponent={<ProjectConfigurationTutorial />}
                        />
                    </Grid>
                </Grid>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h4'>{title}</Typography>
                    </Grid>

                    <GeneralInputsComponent
                        projectSettings={projectSettings}
                        onProjectSettingsChange={handleProjectSettingsChange}
                    />

                    <WorkItemTypesComponent
                        workItemTypes={projectSettings?.workItemTypes || []}
                        onAddWorkItemType={handleAddWorkItemType}
                        onRemoveWorkItemType={handleRemoveWorkItemType}
                    />

                    {!modifyDefaultSettings ? (
                        <>
                            <WorkTrackingSystemComponent
                                workTrackingSystems={workTrackingSystems}
                                selectedWorkTrackingSystem={selectedWorkTrackingSystem}
                                onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
                                onNewWorkTrackingSystemConnectionAdded={handleOnNewWorkTrackingSystemConnectionAddedDialogClosed} />

                            <MilestonesComponent
                                milestones={projectSettings?.milestones || []}
                                onAddMilestone={handleAddMilestone}
                                onRemoveMilestone={handleRemoveMilestone}
                                onUpdateMilestone={handleUpdateMilestone} />
                        </>) :
                        (
                            <></>
                        )}

                    <AdvancedInputsComponent
                        projectSettings={projectSettings}
                        onProjectSettingsChange={handleProjectSettingsChange}
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
    )
}

export default ModifyProjectSettings;