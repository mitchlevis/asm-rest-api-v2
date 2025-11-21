// Facet specifications builder for GET /getLinks
// Values only (no counts). Use concatenated label: RegionName - CategoryName.

const buildFacetSpecs = () => [
  { key: 'CategoryId', attribute: 'LinkCategory.CategoryId', labelAttribute: 'LinkCategory.CategoryId' },
  { key: 'CategoryName', attribute: 'LinkCategory.CategoryName', labelAttribute: 'LinkCategory.CategoryName' },
  { key: 'CategoryDescription', attribute: 'LinkCategory.CategoryDescription', labelAttribute: 'LinkCategory.CategoryDescription' },
  { key: 'SortOrder', attribute: 'LinkCategory.SortOrder', labelAttribute: 'LinkCategory.SortOrder' },
  { key: 'CategoryColor', attribute: 'LinkCategory.CategoryColor', labelAttribute: 'LinkCategory.CategoryColor' },
];

export default buildFacetSpecs;
