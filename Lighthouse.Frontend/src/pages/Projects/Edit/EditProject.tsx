import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
    Button,
    Typography,
    SelectChangeEvent,
    Container,
    Grid
} from '@mui/material';
import { ApiServiceProvider } from '../../../services/Api/ApiServiceProvider';
import { IWorkTrackingSystemConnection } from '../../../models/WorkTracking/WorkTrackingSystemConnection';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import WorkItemTypesComponent from '../../../components/Common/WorkItemTypes/WorkItemTypesComponent';
import WorkTrackingSystemComponent from '../../../components/Common/WorkTrackingSystems/WorkTrackingSystemComponent';
import GeneralInputsComponent from './GeneralInputs';
import AdvancedInputsComponent from './AdvancedInputs';
import { IProjectSettings } from '../../../models/Project/ProjectSettings';
import { IMilestone } from '../../../models/Project/Milestone';
import MilestonesComponent from '../../../components/Common/Milestones/MilestonesComponent';

const EditProjectPage: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewProject = id === undefined;

    const navigate = useNavigate();
    const apiService = ApiServiceProvider.getApiService();

    const [projectSettings, setProjectSettings] = useState<IProjectSettings | null>(null);
    const [workTrackingSystems, setWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [loading, setLoading] = useState<boolean>(false);

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const systems = await apiService.getConfiguredWorkTrackingSystems();

                if (!isNewProject && id) {
                    const settings = await apiService.getProjectSettings(parseInt(id, 10));
                    setProjectSettings(settings);

                    setSelectedWorkTrackingSystem(systems.find(system => system.id === settings.workTrackingSystemConnectionId) ?? null);
                } else {
                    setProjectSettings({
                        id: 0,
                        name: 'New Project',
                        workItemTypes: ["Epic"],
                        milestones: [],
                        workItemQuery: '',
                        unparentedItemsQuery: '',
                        defaultAmountOfWorkItemsPerFeature: 15,
                        workTrackingSystemConnectionId: 0,
                    });
                }

                setWorkTrackingSystems(systems);
            } catch (error) {
                console.error('Error fetching data', error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [id, isNewProject, apiService]);

    const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
        const selectedWorkTrackingSystemName = event.target.value;
        const selectedWorkTrackingSystem = workTrackingSystems.find(system => system.name === selectedWorkTrackingSystemName) ?? null;

        setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);
    };

    const handleSave = async () => {
        if (!projectSettings) return;

        let updatedSettings: IProjectSettings = { ...projectSettings, workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0 };

        if (isNewProject) {
            updatedSettings = await apiService.createProject(updatedSettings);
        } else {
            updatedSettings = await apiService.updateProject(updatedSettings);
        }

        navigate(`/projects/${updatedSettings.id}`);
    };

    const handleOnNewWorkTrackingSystemConnectionAddedDialogClosed = async (newConnection: IWorkTrackingSystemConnection) => {
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

    const allRequiredFieldsFilled = () => {
        return projectSettings?.name && projectSettings?.defaultAmountOfWorkItemsPerFeature !== undefined &&
            projectSettings?.workItemQuery && projectSettings?.workItemTypes.length > 0 && selectedWorkTrackingSystem !== null;
    };

    return (
        <LoadingAnimation isLoading={loading} hasError={false} >
            <Container>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h4'>{`${projectSettings?.name} Configuration`}</Typography>
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

                    <WorkTrackingSystemComponent
                        workTrackingSystems={workTrackingSystems}
                        selectedWorkTrackingSystem={selectedWorkTrackingSystem}
                        onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
                        onNewWorkTrackingSystemConnectionAdded={handleOnNewWorkTrackingSystemConnectionAddedDialogClosed}
                    />

                    <MilestonesComponent
                        milestones={projectSettings?.milestones || []}
                        onAddMilestone={handleAddMilestone}
                        onRemoveMilestone={handleRemoveMilestone}
                        onUpdateMilestone={handleUpdateMilestone}
                    />

                    <AdvancedInputsComponent
                        projectSettings={projectSettings}
                        onProjectSettingsChange={handleProjectSettingsChange}
                    />

                    <Grid item xs={12}>
                        <Button
                            variant="contained"
                            color="primary"
                            onClick={handleSave}
                            disabled={!allRequiredFieldsFilled()}
                        >
                            {isNewProject ? 'Create' : 'Update'}
                        </Button>
                    </Grid>
                </Grid>
            </Container>
        </LoadingAnimation>
    );
};

export default EditProjectPage;