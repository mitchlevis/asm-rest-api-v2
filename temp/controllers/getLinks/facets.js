// Facet specifications builder for GET /getLinks
// Values only (no counts). Use concatenated label: RegionName - CategoryName.

const buildFacetSpecs = ({ regionModel }) => [
  { key: 'LinkId', attribute: 'Link.LinkId', labelAttribute: 'Link.LinkId' },
  { 
    key: 'CategoryId', 
    attribute: 'Link.CategoryId', 
    labelConcat: { parts: ['Region.RegionName', 'LinkCategory.CategoryName'], separator: ' - ' },
    include: [ { model: regionModel, as: 'Region', required: false } ]
  },
];

export default buildFacetSpecs;
