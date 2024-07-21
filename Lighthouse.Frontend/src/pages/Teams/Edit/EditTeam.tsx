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
import { ITeamSettings } from '../../../models/Team/TeamSettings';
import { IWorkTrackingSystemConnection } from '../../../models/WorkTracking/WorkTrackingSystemConnection';
import LoadingAnimation from '../../../components/Common/LoadingAnimation/LoadingAnimation';
import WorkItemTypesComponent from '../../../components/Common/WorkItemTypes/WorkItemTypesComponent';
import WorkTrackingSystemComponent from '../../../components/Common/WorkTrackingSystems/WorkTrackingSystemComponent';
import GeneralInputsComponent from './GeneralInputs';
import AdvancedInputsComponent from './AdvancedInputs';

const EditTeamPage: React.FC = () => {
    const { id } = useParams<{ id?: string }>();
    const isNewTeam = id === undefined;

    const navigate = useNavigate();
    const apiService = ApiServiceProvider.getApiService();

    const [teamSettings, setTeamSettings] = useState<ITeamSettings | null>(null);
    const [workTrackingSystems, setWorkTrackingSystems] = useState<IWorkTrackingSystemConnection[]>([]);
    const [selectedWorkTrackingSystem, setSelectedWorkTrackingSystem] = useState<IWorkTrackingSystemConnection | null>(null);
    const [loading, setLoading] = useState<boolean>(false);
    const [newWorkItemType, setNewWorkItemType] = useState<string>('');

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const systems = await apiService.getConfiguredWorkTrackingSystems();

                if (!isNewTeam && id) {
                    const settings = await apiService.getTeamSettings(parseInt(id, 10));
                    setTeamSettings(settings);

                    setSelectedWorkTrackingSystem(systems.find(system => system.id === settings.workTrackingSystemConnectionId) ?? null);
                } else {
                    setTeamSettings({
                        id: 0,
                        name: 'New Team',
                        throughputHistory: 30,
                        featureWIP: 1,
                        workItemQuery: '',
                        workItemTypes: ["User Story", "Bug"],
                        workTrackingSystemConnectionId: 0,
                        relationCustomField: ''
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
    }, [id, isNewTeam, apiService]);

    const handleWorkTrackingSystemChange = (event: SelectChangeEvent<string>) => {
        const selectedWorkTrackingSystemName = event.target.value;
        const selectedWorkTrackingSystem = workTrackingSystems.find(system => system.name === selectedWorkTrackingSystemName) ?? null;

        setSelectedWorkTrackingSystem(selectedWorkTrackingSystem);
    };

    const handleSave = async () => {
        if (!teamSettings) return;

        let updatedSettings: ITeamSettings = { ...teamSettings, workTrackingSystemConnectionId: selectedWorkTrackingSystem?.id ?? 0 };

        if (isNewTeam) {
            updatedSettings = await apiService.createTeam(updatedSettings);
        } else {
            updatedSettings = await apiService.updateTeam(updatedSettings);
        }

        navigate(`/teams/${updatedSettings.id}`);
    };

    const handleOnNewWorkTrackingSystemConnectionAddedDialogClosed = async (newConnection: IWorkTrackingSystemConnection) => {
        setWorkTrackingSystems(prevSystems => [...prevSystems, newConnection]);
        setSelectedWorkTrackingSystem(newConnection);
    };

    const handleAddWorkItemType = () => {
        if (newWorkItemType.trim()) {
            setTeamSettings(prev => prev ? { ...prev, workItemTypes: [...(prev.workItemTypes || []), newWorkItemType.trim()] } : prev);
            setNewWorkItemType('');
        }
    };

    const handleRemoveWorkItemType = (type: string) => {
        setTeamSettings(prev => prev ? { ...prev, workItemTypes: (prev.workItemTypes || []).filter(item => item !== type) } : prev);
    };

    const handleTeamSettingsChange = (key: keyof ITeamSettings, value: string | number) => {
        setTeamSettings(prev => prev ? { ...prev, [key]: value } : prev);
    };

    const allRequiredFieldsFilled = () => {
        return teamSettings?.name && teamSettings?.throughputHistory && teamSettings?.featureWIP !== undefined &&
            teamSettings?.workItemQuery && teamSettings?.workItemTypes.length > 0 && selectedWorkTrackingSystem !== null;
    };
    return (
        <LoadingAnimation isLoading={loading} hasError={false} >
            <Container>
                <Grid container spacing={3}>
                    <Grid item xs={12}>
                        <Typography variant='h4'>{`${teamSettings?.name} Configuration`}</Typography>
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

                    <WorkTrackingSystemComponent
                        workTrackingSystems={workTrackingSystems}
                        selectedWorkTrackingSystem={selectedWorkTrackingSystem}
                        onWorkTrackingSystemChange={handleWorkTrackingSystemChange}
                        onNewWorkTrackingSystemConnectionAdded={handleOnNewWorkTrackingSystemConnectionAddedDialogClosed}
                    />

                    <AdvancedInputsComponent
                        teamSettings={teamSettings}
                        onTeamSettingsChange={handleTeamSettingsChange}
                    />

                    <Grid item xs={12}>
                        <Button
                            variant="contained"
                            color="primary"
                            onClick={handleSave}
                            disabled={!allRequiredFieldsFilled()}
                        >
                            {isNewTeam ? 'Create' : 'Update'}
                        </Button>
                    </Grid>
                </Grid>
            </Container>
        </LoadingAnimation>
    );
};

export default EditTeamPage;