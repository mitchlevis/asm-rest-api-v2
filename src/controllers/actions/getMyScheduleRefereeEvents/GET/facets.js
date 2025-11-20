// Facet specifications builder for GET /getMyScheduleRefereeEvents
// Values only (no counts). COALESCE labels as requested.

const buildFacetSpecs = ({ parkModel, regionLeagueModel, teamModel }) => [
  { key: 'ScheduleId', attribute: 'Schedule.ScheduleId', labelAttribute: 'Schedule.ScheduleId' },
  {
    key: 'ParkId',
    attribute: 'Schedule.ParkId',
    labelCoalesce: { parts: ['ParkAssociation.ParkName', 'ParkAssociation.ParkId', 'Schedule.ParkId'] },
    include: [ { model: parkModel, as: 'ParkAssociation', required: false } ]
  },
  {
    key: 'LeagueId',
    attribute: 'Schedule.LeagueId',
    labelCoalesce: { parts: ['RegionLeague.LeagueName', 'RegionLeague.LeagueId', 'Schedule.LeagueId'] },
    include: [ { model: regionLeagueModel, as: 'RegionLeague', required: false } ]
  },
  {
    key: 'AwayTeam',
    attribute: 'Schedule.AwayTeam',
    labelCoalesce: { parts: ['AwayTeamAssociation.TeamName', 'AwayTeamAssociation.TeamId', 'Schedule.AwayTeam'] },
    include: [ { model: teamModel, as: 'AwayTeamAssociation', required: false } ]
  },
  {
    key: 'HomeTeam',
    attribute: 'Schedule.HomeTeam',
    labelCoalesce: { parts: ['HomeTeamAssociation.TeamName', 'HomeTeamAssociation.TeamId', 'Schedule.HomeTeam'] },
    include: [ { model: teamModel, as: 'HomeTeamAssociation', required: false } ]
  },
];

export default buildFacetSpecs;

