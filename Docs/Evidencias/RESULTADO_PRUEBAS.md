# Resultado de las pruebas automáticas — NitroRhythm

Generado: 2026-11-14 · Unity 6000.5.8f1 · Unity Test Framework 1.7.0

> Archivo generado por `Docs/Tools/docs/summarize_tests.py` a partir de
> `Logs/test_results_*.xml`. Las pruebas "Omitida" son generadores de capturas que
> solo corren con la variable `NITRO_SHOT_DIR` definida.

## PlayMode

Total 69 · aprobadas 58 · fallidas 0 · omitidas 11

| Clase | Prueba | Resultado | Duración (s) |
| :-- | :-- | :-- | :-- |
| AnimationAndAmbienceTests | Animator_KeepsKartAndPilotUnderTheRigWithoutShear | OK | 0.09 |
| AnimationAndAmbienceTests | Animator_RollsIntoTurnsAndSquashesOnLanding | OK | 2.17 |
| AnimationAndAmbienceTests | HoverEffect_KeepsTheSelectionColourSetByTheScreen | OK | 0.30 |
| AnimationAndAmbienceTests | Visualizer_BuildsTwoEqualizerWallsThatFollowTheLeader | OK | 0.02 |
| CharacterDisplayAlignmentTests | InRaceKarts_RenderViews | Omitida | 0.01 |
| CharacterDisplayAlignmentTests | PilotIsCenteredOnKart_ForEveryCharacter | OK | 0.04 |
| GameplayEvidenceCapture | CaptureCharacterSelectAndResults | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureControlsGuides | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureCooperativeSplitScreen | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureEveryDomain | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureGameplayHud | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureInterfaceScreens | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureLevelIntroCards | Omitida | 0.00 |
| GameplayEvidenceCapture | CaptureVillainAttacks | Omitida | 0.00 |
| GameplayFlowTests | CaptureFollowCameraView | Omitida | 0.00 |
| GameplayFlowTests | Cooperative_GoalAdvancesEvenIfThePartnerStaysBehind | OK | 23.59 |
| GameplayFlowTests | Cooperative_PlayersShowTheirOwnDistinctKartsAndCameras | OK | 2.64 |
| GameplayFlowTests | FirstLevel_ShowsTheControlsGuide_AndRunsOnceClosed | OK | 5.87 |
| GameplayFlowTests | KnockedOutKart_WaitsFiveSecondsThenRevivesAtCheckpoint | OK | 11.65 |
| GameplayFlowTests | PlayerTwo_IsNeverTheSamePilotAsPlayerOne | OK | 0.00 |
| GameplayFlowTests | ReachingTheGoal_AdvancesToTheNextLevel | OK | 17.09 |
| GameplayFlowTests | RealScene_KartDrivesAndCameraFollowsIt | OK | 8.68 |
| HudAndFeedbackTests | Bot_JumpsOverTrackGaps | OK | 6.03 |
| HudAndFeedbackTests | CameraFollow_ShakeReturnsToRest | OK | 0.41 |
| HudAndFeedbackTests | Checkpoint_RaisesEventAndMovesForwardOnly | OK | 0.00 |
| HudAndFeedbackTests | Hud_ComputeRank_CountsOpponentsAhead | OK | 0.00 |
| HudAndFeedbackTests | Hud_FormatTime_UsesMinutesSecondsCentiseconds | OK | 0.00 |
| HudAndFeedbackTests | Kart_BoostExposesTimeLeft | OK | 0.00 |
| HudAndFeedbackTests | Sfx_ClipsAreSynthesizedAndAudible | OK | 0.01 |
| HudAndFeedbackTests | Track_EveryDomainStartsWithASafeRunway | OK | 0.00 |
| HudAndFeedbackTests | Track_FirstDomainIsGentlerThanTheLast | OK | 0.00 |
| HudAndFeedbackTests | VillainHit_RaisesHitEventAndExposesSlowTimer | OK | 0.02 |
| LegacyEvidenceCapture | CaptureLegacyBefore | Omitida | 0.00 |
| Phase0Tests | Checkpoints_AreFewAndEvenlySpread | OK | 0.00 |
| Phase0Tests | Kart_ModelFacesForward_AndPilotSitsBehindCenter | OK | 0.02 |
| Phase0Tests | Villain_EveryAttackKindRunsWithoutErrors | OK | 3.21 |
| Phase0Tests | Villain_StaysInRangeAndAttacksTheLeadPlayer | OK | 14.01 |
| Phase1Tests | GameSession_RestartRewindsTotalsToTheLevelStart | OK | 0.00 |
| Phase1Tests | GameSession_RunAdvancesThroughAllPlayableDomains | OK | 0.00 |
| Phase1Tests | LevelManager_BuildsOnlyTheRequestedDomain | OK | 0.09 |
| Phase2Tests | Decorator_KeepsEveryPropClearOfTheTrack | OK | 0.50 |
| Phase2Tests | EveryDomain_HasAnAudibleAmbience | OK | 0.19 |
| Phase2Tests | EveryDomain_HasItsOwnMusicThatTheAnalyserUnderstands | OK | 1.73 |
| Phase2Tests | EveryPlayableDomain_HasAThemeWithPropsAndDistinctLook | OK | 0.00 |
| Phase2Tests | Fonts_OrbitronRajdhaniAndIconsAreAvailable | OK | 0.00 |
| Phase2Tests | Hud_BuildsAndUpdatesWithoutErrors | OK | 0.02 |
| Phase2Tests | Results_RankThresholds | OK | 0.00 |
| Phase2Tests | UiFactory_BuildsNeonControls | OK | 0.06 |
| PrototypePlayModeTests | AudioAnalysis_DetectsTempoAndBeats | OK | 0.06 |
| PrototypePlayModeTests | BadReward_TravelsParabola_ThenExplodesAtTarget | OK | 1.21 |
| PrototypePlayModeTests | GameSession_StoresCharacterSelection | OK | 0.00 |
| PrototypePlayModeTests | InputProviders_AreDecoupledFromPhysics | OK | 0.00 |
| PrototypePlayModeTests | Kart_AcceleratesForward_WithScriptedInput | OK | 1.79 |
| PrototypePlayModeTests | Kart_JumpsWhenGrounded | OK | 0.72 |
| PrototypePlayModeTests | LevelManager_GeneratesSevenAudioReactiveDomains | OK | 0.55 |
| PrototypePlayModeTests | PrototypeData_LoadsTenDomainsAndThreeCharacters | OK | 0.00 |
| PrototypePlayModeTests | ReactiveTrack_MapsBassToGapsAndTrebleToObstacles | OK | 0.00 |
| PrototypePlayModeTests | VillainBoss_DrivesForward | OK | 1.00 |
| UserSongTests | Analysis_FindsTheTempoOfRawSamples | OK | 0.02 |
| UserSongTests | CleanName_DropsExtensionAndUnderscores | OK | 0.00 |
| UserSongTests | Loader_ReadsALocalWavFile | OK | 0.10 |
| UserSongTests | Loader_ReadsAnMp3File | OK | 0.05 |
| UserSongTests | Loader_ReportsAMissingFile | OK | 0.00 |
| UserSongTests | MusicController_PlaysTheDomainSectionOfTheUserSong | OK | 0.20 |
| UserSongTests | SectionAnalysis_KeepsTempoAndBuildsATrack | OK | 0.15 |
| UserSongTests | SectionClip_HasTheSectionLengthAndFadedEdges | OK | 0.10 |
| UserSongTests | Sections_OfAShortSongOverlapButKeepAPlayableLength | OK | 0.07 |
| UserSongTests | Sections_SplitALongSongInOrderFromStartToEnd | OK | 0.28 |
| UserSongTests | ToMono_AveragesChannelsAndCapsTheLength | OK | 0.00 |

## EditMode

Total 4 · aprobadas 4 · fallidas 0 · omitidas 0

| Clase | Prueba | Resultado | Duración (s) |
| :-- | :-- | :-- | :-- |
| SceneSetupTests | BlenderModelsAreImported | OK | 0.04 |
| SceneSetupTests | FourPrototypeScenesExist | OK | 0.01 |
| SceneSetupTests | NarrativeDatabaseIsImported | OK | 0.00 |
| SceneSetupTests | ScenesAreRegisteredInBuildSettings | OK | 0.00 |

## Resumen global

**62 aprobadas, 0 fallidas, 11 omitidas de 73.** Línea base anterior a esta ronda de ajustes: 14 pruebas.

### Cómo reproducirlo

```bash
UNITY=/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity
flatpak run --command="$UNITY" com.unity.UnityHub -batchmode -nographics \
  -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults Logs/test_results_playmode.xml
flatpak run --command="$UNITY" com.unity.UnityHub -batchmode -nographics \
  -projectPath "$PWD" -runTests -testPlatform EditMode -testResults Logs/test_results_editmode.xml
```
