from libraries.schemas.training_artifact import TrainingArtifact


artifact = TrainingArtifact()

artifact.header = TrainingArtifact.Header()
artifact.header.num_layers = 2
artifact.header.num_epochs = 10
artifact.header.num_samples = 100
artifact.header.num_features = 4
