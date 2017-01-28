﻿using RimWorld;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.Detours {
	public static class _WorkGiver_DoBill {
		public static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingAmount> chosen) {
			Type WorkGiver_DoBill_Type = typeof(WorkGiver_DoBill);

			chosen.Clear();
			if (bill.recipe.ingredients.Count == 0) {
				return true;
			}
			IntVec3 billGiverRootCell = ReflectionUtils.execStaticPrivate<IntVec3>(WorkGiver_DoBill_Type, "GetBillGiverRootCell", billGiver, pawn);
			Region validRegionAt = pawn.Map.regionGrid.GetValidRegionAt(billGiverRootCell);
			if (validRegionAt == null) {
				return false;
			}
			ReflectionUtils.execStaticPrivate(WorkGiver_DoBill_Type, "MakeIngredientsListInProcessingOrder", ReflectionUtils.getStaticPrivateValue<List<IngredientCount>>(WorkGiver_DoBill_Type, "ingredientsOrdered"), bill);
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings").Clear();
			bool foundAll = false;
			Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.recipe.fixedIngredientFilter.Allows(t) && bill.ingredientFilter.Allows(t) && bill.recipe.ingredients.Any((IngredientCount ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t, 1) && (!bill.CheckIngredientsIfSociallyProper || t.IsSociallyProper(pawn)) && t.fowIsVisible();
			bool billGiverIsPawn = billGiver is Pawn;
			if (billGiverIsPawn) {
				ReflectionUtils.execStaticPrivate(WorkGiver_DoBill_Type, "AddEveryMedicineToRelevantThings", pawn, billGiver, ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), baseValidator, pawn.Map);
				if (ReflectionUtils.execStaticPrivate<bool>(WorkGiver_DoBill_Type, "TryFindBestBillIngredientsInSet", ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), bill, chosen)) {
					return true;
				}
			}
			TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
			RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, false);
			RegionProcessor regionProcessor = delegate (Region r) {
				ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Clear();
				List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
				for (int i = 0; i < list.Count; i++) {
					Thing thing = list[i];
					if (baseValidator(thing) && (!thing.def.IsMedicine || !billGiverIsPawn)) {
						ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Add(thing);
					}
				}
				if (ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Count > 0) {
					Comparison<Thing> comparison = delegate (Thing t1, Thing t2) {
						float lengthHorizontalSquared = (t1.Position - pawn.Position).LengthHorizontalSquared;
						float lengthHorizontalSquared2 = (t2.Position - pawn.Position).LengthHorizontalSquared;
						return lengthHorizontalSquared.CompareTo(lengthHorizontalSquared2);
					};
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Sort(comparison);
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings").AddRange(ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings"));
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Clear();
					if (ReflectionUtils.execStaticPrivate<bool>(WorkGiver_DoBill_Type, "TryFindBestBillIngredientsInSet", ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), bill, chosen)) {
						foundAll = true;
						return true;
					}
				}
				return false;
			};
			RegionTraverser.BreadthFirstTraverse(validRegionAt, entryCondition, regionProcessor, 99999);
			return foundAll;
		}
	}
}