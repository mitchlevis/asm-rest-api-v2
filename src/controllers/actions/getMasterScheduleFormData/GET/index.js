import { authenticateSessionToken, validateIncomingParameters, getSequelizeObject, formatSuccessResponse, formatErrorResponse } from '../../../../utils/helpers';
import * as requestValidationSchema from "./request";

export default async (request) => {
	try{
		// Authenticate Session Token
		const tokenData = await authenticateSessionToken(request);
		const userId = tokenData.UserName;

		// Validate Parameters
		const { path, query, body } = await validateIncomingParameters(request, requestValidationSchema);

		const { regionId } = path;
		const { show_archived: showArchived } = query;
console.log('showArchived', showArchived);
		const startTime = Date.now();
		console.log('Fetching Data');

		// Get database connection for raw queries
		const modelLoadStart = Date.now();
		const sequelize = await getSequelizeObject(request);
		console.log(`[TIMING] Loading models took ${Date.now() - modelLoadStart}ms`);

		// Step 1: Get user's regions
		console.log('Fetching User Regions');
		const userRegionsQueryStart = Date.now();
		const userRegionsQuery = `
			SELECT RegionId, IsExecutive
			FROM RegionUser
			WHERE RealUsername = :username
				AND IsArchived = :showArchived
		`;
		const replacements = { username: userId, showArchived: showArchived ? 1 : 0 };
		console.log('[QUERY] userRegionsQuery:', userRegionsQuery);
		console.log('[QUERY] replacements:', replacements);
		const userRegions = await sequelize.query(userRegionsQuery, {
			type: sequelize.QueryTypes.SELECT,
			replacements
		});
		console.log(`[TIMING] User regions query took ${Date.now() - userRegionsQueryStart}ms`);

		if (userRegions.length === 0) {
			return formatSuccessResponse(request, {
				data: {
					regions: {},
					regionLeagues: {},
					distinctScheduleValues: { scheduleId: [], parkId: [], leagueId: [], teamId: [] }
				}
			});
		}

		const regionIds = userRegions.map(r => r.RegionId);
		const regionIdsString = regionIds.map(id => `N'${id}'`).join(',');

		// Step 2-6: Run all data fetching queries in parallel
		console.log('Fetching all data in parallel');
		const parallelQueriesStart = Date.now();

		const [
			regionsData,
			regionLeaguesData,
			realLeagueTeams,
			realLeagueParks,
			regionTeams,
			regionParks
		] = await Promise.all([
			// Get Region data
			sequelize.query(`
				SELECT RegionID, RegionId
				FROM Region
				WHERE RegionID IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT }),

			// Get RegionLeagues
			sequelize.query(`
				SELECT *
				FROM RegionLeague
				WHERE RegionId IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT }),

			// Get Teams from RealLeagues
			sequelize.query(`
				SELECT t.*, rl.RegionId as ParentRegionId, rl.LeagueId
				FROM RegionLeague rl
				INNER JOIN Team t ON t.RegionId = rl.RealLeagueId
				WHERE rl.RegionId IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT }),

			// Get Parks from RealLeagues
			sequelize.query(`
				SELECT p.*, rl.RegionId as ParentRegionId, rl.LeagueId
				FROM RegionLeague rl
				INNER JOIN Park p ON p.RegionId = rl.RealLeagueId
				WHERE rl.RegionId IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT }),

			// Get Teams from Regions
			sequelize.query(`
				SELECT TeamId, TeamName, RegionId
				FROM Team
				WHERE RegionId IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT }),

			// Get Parks from Regions
			sequelize.query(`
				SELECT ParkId, ParkName, RegionId
				FROM Park
				WHERE RegionId IN (${regionIdsString})
			`, { type: sequelize.QueryTypes.SELECT })
		]);

		console.log(`[TIMING] Parallel queries took ${Date.now() - parallelQueriesStart}ms`);

		// Step 7: Assemble the data structure
		console.log('Assembling data structures');
		const assemblyStart = Date.now();

		// Build regions object
		const regions = {};
		for (const regionData of regionsData) {
			regions[regionData.RegionID] = {
				RegionId: regionData.RegionId,
				Teams: regionTeams.filter(t => t.RegionId === regionData.RegionID),
				Parks: regionParks.filter(p => p.RegionId === regionData.RegionID)
			};
		}

		// Build regionLeagues object
		const regionLeagues = {};
		for (const userRegion of userRegions) {
			const leagues = regionLeaguesData.filter(rl => rl.RegionId === userRegion.RegionId);
			regionLeagues[userRegion.RegionId] = {};

			for (const league of leagues) {
				// Attach RealLeague data
				league.RealLeague = {
					RegionId: league.RealLeagueId,
					Teams: realLeagueTeams.filter(t =>
						t.ParentRegionId === league.RegionId &&
						t.LeagueId === league.LeagueId
					),
					Parks: realLeagueParks.filter(p =>
						p.ParentRegionId === league.RegionId &&
						p.LeagueId === league.LeagueId
					)
				};

				regionLeagues[userRegion.RegionId][league.LeagueId] = league;
			}
		}

		console.log(`[TIMING] Data assembly took ${Date.now() - assemblyStart}ms`)

		// Compile Teams for distinctScheduleValues
		console.log('Compiling Teams for filters');
		const teamsCompileStart = Date.now();
		let teams = [];

		for (const userRegion of userRegions) {
			// If regionId is specified, filter by that region
			if (regionId && regionId !== userRegion.RegionId) {
				continue;
			}

			const regionLeaguesList = regionLeaguesData.filter(rl => rl.RegionId === userRegion.RegionId);

			for (const league of regionLeaguesList) {
				const leagueTeams = realLeagueTeams.filter(t =>
					t.ParentRegionId === league.RegionId &&
					t.LeagueId === league.LeagueId
				);

				for (const team of leagueTeams) {
					teams.push({
						value: team.TeamId,
						label: `${team.TeamName} - (${team.TeamId})`
					});
				}
			}
		}
		console.log(`[TIMING] Compiling teams took ${Date.now() - teamsCompileStart}ms`);

		// Distinct Schedule Values (Used in filter)
		const filterRegionIds = regionId ? [regionId] : regionIds;
		const filterRegionIdsString = filterRegionIds.map(id => `N'${id}'`).join(',');

		console.log('Fetching distinct schedule values');
		const distinctValuesStart = Date.now();

		const [scheduleId, parkId, leagueId] = await Promise.all([
			// Schedules
			sequelize.query(`
				SELECT DISTINCT ScheduleId AS value
				FROM Schedule
				WHERE RegionId IN (${filterRegionIdsString})
				ORDER BY ScheduleId ASC
			`, { type: sequelize.QueryTypes.SELECT }),

			// Parks
			sequelize.query(`
				SELECT DISTINCT s.ParkId AS value, p.ParkName AS label
				FROM Schedule s
				LEFT JOIN Park p ON p.ParkId = s.ParkId
				WHERE s.RegionId IN (${filterRegionIdsString})
				ORDER BY s.ParkId ASC
			`, { type: sequelize.QueryTypes.SELECT }),

			// Leagues
			sequelize.query(`
				SELECT DISTINCT LeagueId AS value,
					CONCAT(LeagueName, N' - (', LeagueId, N')') AS label
				FROM RegionLeague
				WHERE RegionId IN (${filterRegionIdsString})
				ORDER BY LeagueId ASC
			`, { type: sequelize.QueryTypes.SELECT })
		]);

		console.log(`[TIMING] Distinct values queries took ${Date.now() - distinctValuesStart}ms`);

		console.log('Data fetching complete');
		const totalTime = Date.now() - startTime;
		console.log(`[TIMING] ========================================`);
		console.log(`[TIMING] TOTAL EXECUTION TIME: ${totalTime}ms`);
		console.log(`[TIMING] ========================================`);

		const data = {
			regions,
			regionLeagues,
			distinctScheduleValues: { scheduleId, parkId, leagueId, teamId: teams }
		};

		return formatSuccessResponse(request, { data });
	}
	catch(err){
		console.error(err);
		return formatErrorResponse(request, err);
	}
};

